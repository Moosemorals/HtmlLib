using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uk.osric.HtmlLib;

namespace uk.osric.HtmlLib.Html {
    internal class Document : Node, IDocument {

        private readonly string _type;
        private DocumentType? _doctype;
        private Element? _documentElement;

        internal Document(
            string encoding = "utf-8",
            string contentType = "application/xml",
            string type = "xml",
            string mode = "no-quirks",
            string url = "about:blank"

            ) : base("#document") {

            _type = type;
            CharacterSet = encoding;
            URL = url;
            ContentType = contentType;
            CompatMode = mode == "quirks" ? "BackCompat" : "CSS1Compat";
        }

        public override NodeType NodeType => NodeType.Document;

        public override string? TextContent => null;

        public string URL { get; internal set; }

        public string CompatMode { get; init; }

        public string CharacterSet { get; init; }

        public string ContentType { get; init; }

        public IDocumentType? Doctype {
            get => _doctype;
            set {
                if (_doctype != null) {
                    RemoveChild(_doctype);
                }
                _doctype = value as DocumentType;
                if (_doctype != null) {
                    AppendChild(_doctype);
                }
            }
        }

        public IElement? DocumentElement {
            get => _documentElement;

            set {
                if (_documentElement != null) {
                    RemoveChild(_documentElement);
                }
                _documentElement = value as Element;
                if (_documentElement != null) {
                    AppendChild(_documentElement);
                }
            }
        }

        public override INode CloneNode(bool deep = false) => throw new NotImplementedException();
        public IAttr CreateAttribute(string localName) => throw new NotImplementedException();
        public IDocumentFragment CreateDocumentFragment() => throw new NotImplementedException();

        public Element CreateElement(string localName) => new(localName.ToLowerInvariant()) { OwnerDocument = this };
        internal Element CreateElement(TagToken tag) {
            Element el = CreateElement(tag.Name);
            foreach (var a in tag.Attr) {

            }

            return el;
        }

        IElement IDocument.CreateElement(string localName) => CreateElement(localName);


        public IText CreateTextNode(string data) => throw new NotImplementedException();
        public IElement? GetElementById(string elementId) => throw new NotImplementedException();
        public IHTMLCollection GetElementsByClassName(string classNames) => throw new NotImplementedException();
        public IHTMLCollection GetElementsByTagName(string qualifiedName) => throw new NotImplementedException();
        public INode ImportNode(INode original, bool deep = false) => throw new NotImplementedException();
    }
}
