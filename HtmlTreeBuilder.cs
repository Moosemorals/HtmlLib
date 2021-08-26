using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uk.osric.HtmlLib.Html;

using static uk.osric.HtmlLib.CharacterTests;

namespace uk.osric.HtmlLib
{
    class HtmlTreeBuilder
    {
        private readonly Document _doc;

        public HtmlTreeBuilder(string body)
        {
            _tokens = new HtmlTokeniser(body);
            _doc = new Document();
        }

        private readonly Stack<Element> _openElements = new();
        private Element _currentNode => _openElements.Peek();

        private readonly HtmlTokeniser _tokens;
        private HtmlToken NextToken() => _tokens.NextToken();
        private bool _isFosterParenting = false;

        private delegate InsertionMode InsertionMode();

        private Element CreateElement(TagToken token) => _doc.CreateElement(token.Name);


        public IDocument Parse()
        {
            InsertionMode _currentMode = Initial;

            while (_currentMode != Done)
            {
                _currentMode = _currentMode();
            }

            return _doc;
        }

        private InsertionMode Done() => Done;

        private InsertionMode Initial()
        {
            while (true)
            {
                HtmlToken t = _tokens.NextToken();
                switch (t)
                {
                    case CharacterToken ct when ct.Is(IsWhitespace):
                        // ignored
                        break;
                    case CommentToken comment:
                        // TOOD: Comment nodes
                        break;
                    case DoctypeToken dt:
                        _doc.Doctype = new DocumentType(dt.Name, dt.Public ?? "", dt.System ?? "");
                        // TODO: Check for quirks mode
                        return BeforeHtml;
                    default:
                        _tokens.PushBack(t);
                        return BeforeHtml;
                }
            }
        }


        private InsertionMode BeforeHtml()
        {

            while (true)
            {
                HtmlToken t = _tokens.NextToken();
                switch (t)
                {
                    case DoctypeToken:
                        // ignored
                        break;
                    case CommentToken comment:
                        // TODO: Comment nodes
                        break;
                    case CharacterToken ct when ct.Is(IsWhitespace):
                        // ignored
                        break;
                    case StartTagToken st when TagNameIs(st, "html"):
                        {
                            Element html = _doc.CreateElement(st);
                            _openElements.Push(html);
                            return BeforeHead;

                        }
                    case EndTagToken et when TagNameIs(et, "head", "body", "html", "br"):

                    case EndTagToken:
                        // Parse error
                        // ignored
                        break;
                    default:

                        break;

                }


            }
        }


        private InsertionMode BeforeHead() { return Done; }

        private static bool TagNameIs(TagToken t, params string[] names) => names.Contains(t.Name);


    }
}
