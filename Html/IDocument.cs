using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface IDocument : INode {

        // public IDOMImplementation Implementation {get;}

        public string URL { get; }

        public string DocumentURI => URL;
        public string CompatMode { get; }

        public string CharacterSet { get; }
        public string Charset => CharacterSet;
        public string InputEncoding => CharacterSet;

        public string ContentType { get; }

        public IDocumentType? Doctype { get; }

        public IElement? DocumentElement { get; }

        public IHTMLCollection GetElementsByTagName(string qualifiedName);
        public IHTMLCollection GetElementsByClassName(string classNames);
        public IElement? GetElementById(string elementId);

        public IElement CreateElement(string localName);

        public IDocumentFragment CreateDocumentFragment();
        public IText CreateTextNode(string data);

        public INode ImportNode(INode original, bool deep = false);

        public IAttr CreateAttribute(string localName);


    }
}
