using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    internal class Text : Node, IText {

        private string _data;

        protected Text(string data = "") : base("#text") {
            _data = data;
        }

        public string WholeText => throw new NotImplementedException();

        public override NodeType NodeType => NodeType.Text;

        public override string? TextContent => _data;

        public override INode CloneNode(bool deep = false) => throw new NotImplementedException();

        // TODO: Handle long strings
        public IText SplitText(int offset) {
            if (offset < 0 || offset > _data.Length) {
                throw new DOMException("IndexSizeError");
            }
            string newData = _data[offset..];

            _data = _data[..offset];

            if (OwnerDocument == null) {
                throw new DOMException("HierarchyRequestError");
            }

            IText newNode = OwnerDocument.CreateTextNode(newData);

            if (ParentNode != null) {
                ParentNode.InsertBefore(newNode, NextSibling);
            }

            return newNode;
        }
    }
}
