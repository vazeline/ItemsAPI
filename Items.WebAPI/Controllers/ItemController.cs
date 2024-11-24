using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Items.BusinessLogic.Services.Actions.Interfaces;
using Items.DTO.Items.Request;
using Items.WebAPI.Filters.Swagger;
using Common.Models.Interfaces;
using Items.Common.WebAPI.ModelValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ItemDTO = Items.DTO.Items.Request.ItemDTO;

namespace Items.WebAPI.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemController : Items.Common.WebAPI.ControllerBase
    {
        private readonly IItemBusinessLogic itemBusinessLogic;

        public ItemController(IItemBusinessLogic actionBusinessLogic)
            : base()
        {
            this.itemBusinessLogic = actionBusinessLogic;
        }

        /// <summary>
        /// Action for Todo Actions.
        /// </summary>
        [HttpGet("list")]
        [PagingSortingOperationFilter(isPagingRequired: true)]
        [ValidateModel]
        [AllowAnonymous]
        public async Task<IActionResult> GetItemsListAsync(
            [FromQuery]
            [Range(0, double.MaxValue, ErrorMessage = "Acceptable values are positive")]
            int? number,
            [FromQuery] int? code,
            [FromQuery] string value)
        {
            var result = await this.itemBusinessLogic.ListAndMapAsync<ItemDTO>(
                listFuncAsync: repository => repository.GlobalListAsync(
                    pagingSorting: IPagingSortingRequest.FromQueryString(this.HttpContext.Request.QueryString.Value),
                    code: code,
                    value: value));

            return this.HandleResult(result);
        }

        [HttpPost("insert")]
        [ValidateModel]
        [AllowAnonymous]
        public async Task<IActionResult> BulkInsertItemsAsync(ItemsInsertRequestDTO dto)
        {
            var result = await this.itemBusinessLogic.BulkInsertValuesAsync(dto.Items.Select(x => (x.Code, x.Value)).ToList());

            return this.HandleResult(result);
        }
    }
}