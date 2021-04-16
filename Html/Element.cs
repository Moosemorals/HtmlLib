using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace uk.osric.HtmlLib.Html {
    internal class Element : Node, IElement {

        private readonly NamedNodeMap _attr;

        public Element(string nodeName) : base(nodeName.ToUpperInvariant()) {
            _attr = new(this);
        }

        public override NodeType NodeType => NodeType.Element;

        public override string? TextContent => string.Join(string.Empty, Decendants.Where(n => n.NodeType == NodeType.Text).Select(t => t.NodeValue));

        public string NamespaceURI => throw new NotImplementedException();

        public string Prefix => throw new NotImplementedException();

        public string Id {
            get => GetAttribute("id") ?? "";
            set => SetAttribute("id", value ?? "");
        }

        public string ClassName {
            get => GetAttribute("class") ?? "";
            set => SetAttribute("class", value ?? "");
        }


        public INamedNodeMap Attributes => _attr;

        public override INode CloneNode(bool deep = false) => throw new NotImplementedException();
        public List<string> GetAttributeNames() => _attr.Names;
        public string? GetAttribute(string qualifiedName) => _attr.GetNamedItem(qualifiedName)?.Value;
        public bool HasAttribute(string qualifiedName) => _attr.Has(qualifiedName);
        public void RemoveAttribute(string qualifiedName) => _attr.RemoveNamedItem(qualifiedName);
        public void SetAttribute(string qualifiedName, string value) => _attr.SetNamedItem(new Attr(qualifiedName, value));
        public bool ToggleAttribute(string qualifiedName, bool? force = null) => throw new NotImplementedException();
        public IAttr? GetAttributeNode(string qualifiedName) => _attr.GetNamedItem(qualifiedName);
        public IAttr? SetAttributeNode(IAttr attr) => _attr.SetNamedItem(attr);
        public IAttr RemoveAttributeNode(IAttr attr) {
            if (!_attr.Has(attr.Name)) {
                throw new DOMException("NotFoundError");
            }

            _attr.RemoveNamedItem(attr.Name);

            return attr;
        }
    }
}
