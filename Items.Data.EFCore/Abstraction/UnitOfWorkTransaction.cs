using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Common.Models;
using Items.Data.EFCore.Abstraction.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Items.Data.EFCore.Abstraction
{
    public sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly UnitOfWork unitOfWork;

        private List<OperationResult> transactionResults = new List<OperationResult>();
        private IDbContextTransaction transaction;
        private bool isTransactionCommitted;
        private bool isTransactionRollingBack;
        private bool isTransactionRolledBack;

        public UnitOfWorkTransaction(UnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Adds a result to the list of tracked operation results. If the transaction is rolled back, then an attempt to roll back these results will also be made,
        /// starting from the most recently added going backwards.
        /// </summary>
        public void AddResult(OperationResult result)
        {
            if (this.isTransactionRollingBack)
            {
                return;
            }

            if (this.isTransactionCommitted)
            {
                throw new InvalidOperationException("Transaction is already committed");
            }

            if (this.isTransactionRolledBack)
            {
                throw new InvalidOperationException("Transaction is already rolled back");
            }

            this.transactionResults.Add(result);
        }

        public async Task CommitAsync(OperationResult resultToAddErrorsToOnRollbackFailure = null)
        {
            this.transaction.ThrowIfNull("No transaction is in progress");

            var exceptions = new List<Exception>();

            try
            {
                await this.transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception("Error committing database transaction", ex));

                try
                {
                    await this.TryRollbackAsync(resultToAddErrorsToOnRollbackFailure);
                }
                catch (Exception ex2)
                {
                    exceptions.Add(ex2);
                }
            }

            this.isTransactionCommitted = true;

            this.unitOfWork.ResetTransaction();
            this.transactionResults = new List<OperationResult>();

            if (exceptions.Any())
            {
                if (exceptions.Count == 1)
                {
                    throw exceptions[0];
                }

                throw new AggregateException("One or errors occurred attempting to commit the transaction", exceptions);
            }
        }

        /// <summary>
        /// Attempts to rollback the transaction at database level, and will also iterate through a list of all operation results captured by this transaction,
        /// and if they have a rollback action defined, call it. If a transaction is in process in a unit of work, then any business logic methods
        /// executed through their GenericBusinessLogic wrappers will have their results automatically added to the list.
        /// If the transaction has already been rolled back, nothing will happen.
        /// </summary>
        /// <param name="resultToAddErrorsToOnRollbackFailure">If operation result errors occur during rollback, they will be added to the operation result specified in this parameter.</param>
        public async Task TryRollbackAsync(OperationResult resultToAddErrorsToOnRollbackFailure = null)
        {
            this.transaction.ThrowIfNull("No transaction is in progress");

            if (this.isTransactionCommitted)
            {
                throw new InvalidOperationException("Cannot rollback a committed transaction");
            }

            if (this.isTransactionRolledBack)
            {
                return;
            }

            this.isTransactionRollingBack = true;

            var exceptions = new List<Exception>();

            try
            {
                await this.transaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                exceptions.Add(new Exception("Error rolling back database transaction", ex));
            }

            foreach (var transactionResult in Enumerable.Reverse(this.transactionResults))
            {
                try
                {
                    await transactionResult.RollbackAsync(resultToAddErrorsToOnRollbackFailure);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new Exception("Error rolling back operation result", ex));
                }
            }

            this.isTransactionRolledBack = true;
            this.isTransactionRollingBack = false;

            this.unitOfWork.ResetTransaction();
            this.transactionResults = new List<OperationResult>();

            if (exceptions.Any())
            {
                if (exceptions.Count == 1)
                {
                    throw exceptions[0];
                }

                throw new AggregateException("One or errors occurred attempting to rollback the transaction", exceptions);
            }
        }

        internal async Task BeginTransactionAsync()
        {
            if (this.transaction != null)
            {
                throw new InvalidOperationException("Transaction has already been started");
            }

            // we shouldn't ever hit these next 2 checks if the logic works, but belt & braces
            if (this.isTransactionCommitted)
            {
                throw new InvalidOperationException("Transaction has already been committed");
            }

            if (this.isTransactionRolledBack)
            {
                throw new InvalidOperationException("Transaction has already been rolled back");
            }

            this.transaction = await this.unitOfWork.GetContext().Database.BeginTransactionAsync();
        }
    }
}
