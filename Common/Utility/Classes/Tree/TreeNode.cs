using System.Collections.Generic;

namespace Common.Utility.Classes.Tree
{
    public class TreeNode<T>
    {
        public TreeNode(T value, TreeNode<T> parentNode = null)
        {
            this.Value = value;
            this.ParentNode = parentNode;
        }

        public T Value { get; private set; }

        public TreeNode<T> ParentNode { get; }

        public List<TreeNode<T>> ChildNodes { get; } = new List<TreeNode<T>>();

        public IEnumerable<T> ParentNodesValueHierarchy
        {
            get
            {
                var node = this;

                while (node.ParentNode != null)
                {
                    yield return node.ParentNode.Value;
                    node = node.ParentNode;
                }
            }
        }

        public override string ToString() => $"{nameof(this.Value)}: {this.Value} - {nameof(this.ChildNodes)}: {this.ChildNodes.Count}";
    }
}
