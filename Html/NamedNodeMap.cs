using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    internal class NamedNodeMap : INamedNodeMap {

        private readonly List<IAttr> _attr = new();
        private readonly IElement _element;

        public NamedNodeMap(IElement element) {
            _element = element;
        }

        public long Length => _attr.Count;

        public IAttr? GetNamedItem(string qualifiedName) => _attr.FirstOrDefault(a => a.Name == qualifiedName);

        internal bool Has(string qualifiedName) => _attr.Any(a => a.Name == qualifiedName);
        internal List<string> Names => _attr.Select(a => a.Name).ToList();

        public IAttr? Item(long index) => index >= 0 && index < _attr.Count && index < int.MaxValue ? _attr.ElementAt((int)index) : null;

        public IAttr? SetNamedItem(IAttr attr) {
            if (attr.OwnerElement != null && attr.OwnerElement != _element) {
                throw new DOMException("InUseAttributeError");
            }

            IAttr? oldAttr = GetNamedItem(attr.Name);

            if (oldAttr == attr) {
                return attr;
            }
            if (attr is Node attrNode) {
                attrNode.ParentNode = _element;
            } else {
                throw new Exception("Can't set parent node for attribute");
            }

            if (oldAttr != null) {
                int index = _attr.FindIndex(a => a.Name == oldAttr.Name);
                _attr.RemoveAt(index);
                _attr.Insert(index, attr);

                if (oldAttr is Node oldAttrNode) {
                    oldAttrNode.ParentNode = null;
                } else {
                    throw new Exception("Can't clear parent node for old attribute");
                }
            } else {
                _attr.Add(attr);
            }

            return oldAttr;
        }
        public IAttr? RemoveNamedItem(string qualifiedName) {
            IAttr? oldAttr = GetNamedItem(qualifiedName);
            if (oldAttr != null) {
                _attr.Remove(oldAttr);
            }
            return oldAttr;
        }

        public IAttr? GetNamedItemNS(string? @namespace, string localName) => throw new NotImplementedException();
        public IAttr? SetNamedItemNS(IAttr attr) => throw new NotImplementedException();
        public IAttr? RemoveNamedItemNS(string @namespace, string localName) => throw new NotImplementedException();

    }
}
