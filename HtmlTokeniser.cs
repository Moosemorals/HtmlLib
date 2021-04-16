using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Serilog;

using static uk.osric.HtmlLib.CharacterTests;

namespace uk.osric.HtmlLib {
    public class HtmlTokeniser {

        // TODO: Change _tokenStack to a queue
        // TODO: Error handling (or reporting, at least)
        // TODO: Finish adding markup stuff.  


        static HtmlTokeniser() {
            _namedEntities  = LoadNamedEntityReferences();
        }

        public HtmlTokeniser(string text) {
            _text = text;
            _currentState = DataState;
        }

        public HtmlToken NextToken() {

            if (_pushback != null) {
                HtmlToken t = _pushback;
                _pushback = null;
                return t;
            }

            if (_tokenQueue.Count > 0) {
                return _tokenQueue.Dequeue();
            }

            while (!IsEnd()) {
                HtmlToken? next = _currentState();
                if (next != null) {

                    if (next is StartTagToken s) {
                        _currentTag = s;
                    }

                    return next;
                }
            }
            return Emit(EOF);
        }

        private HtmlToken? _pushback;

        public void PushBack(HtmlToken token) {
            _pushback = token;
        }

        public void SetScriptState() {
            _currentState = ScriptDataState;
        }

        private HtmlToken? DataState() {
            int c = Advance();

            switch (c) {
            case '&':
                _currentState = CharacterReferenceState;
                _returnState = DataState;
                return null;
            case '<':
                _currentState = TagOpenState;
                return null;
            case '\0':
                Error("unexpected-null-character");
                return Emit(c);
            case EOF:
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? RCDATAState() {
            int c = Advance();
            switch (c) {
            case '&':
                _currentState = CharacterReferenceState;
                _returnState = RCDATAState;
                return null;
            case '<':
                _currentState = RCDATALessThanSignState;
                return null;
            case '\0':
                Error("unexpected-null-character");
                return Emit('\uFFFD');
            case EOF:
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? RAWTEXTState() {
            int c = Advance();
            switch (c) {
            case '<':
                _currentState = RAWTEXTLessThanSignState;
                return null;
            case '\0':
                Error("unexpected-null-character");
                return Emit('\uFFFD');
            case EOF:
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? ScriptDataState() {
            int c = Advance();
            switch (c) {
            case '<':
                _currentState = ScriptDataLessThanSignState;
                return null;
            case '\0':
                Error("unexpected-null-character");
                return Emit('\uFFFD');
            case EOF:
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? PLAINTEXTState() {
            int c = Advance();
            switch (c) {
            case '\0':
                Error("unexpected-null-character");
                return Emit('\uFFFD');
            case EOF:
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? TagOpenState() {
            int c = Advance();
            switch (c) {
            case '!':
                _currentState = MarkupDeclarationOpenState;
                return null;
            case '/':
                _currentState = EndTagOpenState;
                return null;
            case int when IsAlpha(c):
                _currentToken = new StartTagToken("", NewAttr());
                _reconsume = c;
                _currentState = TagNameState;
                return null;
            case '?':
                Error("unexpected-question-mark-instead-of-tag-name");
                _currentToken = new CommentToken("");
                _reconsume = c;
                _currentState = BogusCommentState;
                return null;
            case EOF:
                Error("eof-before-tag-name");
                return Emit('<', EOF);
            default:
                Error("invalid-first-character-of-tag-name");
                _reconsume = c;
                _currentState = DataState;
                return Emit('<');
            }
        }

        private HtmlToken? EndTagOpenState() {
            int c = Advance();

            switch (c) {
            case int when IsAlpha(c):
                _currentToken = new EndTagToken("", NewAttr());
                _reconsume = c;
                _currentState = TagNameState;
                return null;
            case '>':
                Error("missing-end-tag-name");
                _currentState = DataState;
                return null;
            case EOF:
                Error("eof-before-tag-name");
                return Emit('<', '/', EOF);
            default:
                Error("invalid-first-character-of-tag-name");
                _currentToken = new CommentToken("");
                _reconsume = c;
                _currentState = BogusCommentState;
                return null;
            }
        }

        private HtmlToken? TagNameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    _currentState = BeforeAttributeNameState;
                    return null;
                case '/':
                    _currentState = SelfClosingStartTagState;
                    return null;
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case int when IsUpperAlpha(c):
                    ( _currentToken as TagToken )?.AddToName((char)( c + 0x20 ));
                    break;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as TagToken )?.AddToName('\uFFFD');
                    break;
                case EOF:
                    Error("eof-in-tag");
                    return Emit(EOF);
                default:
                    ( _currentToken as TagToken )?.AddToName((char)c);
                    break;
                }
            }
        }

        private HtmlToken? RCDATALessThanSignState() {
            int c = Advance();
            switch (c) {
            case '/':
                _tempBuffer = "";
                _currentState = RCDATAEndTagOpenState;
                return null;
            default:
                _reconsume = c;
                _currentState = RCDATAState;
                return Emit('<');
            }
        }

        private HtmlToken? RCDATAEndTagOpenState() {
            int c = Advance();
            switch (c) {
            case int when IsAlpha(c):
                _currentToken = new EndTagToken("", NewAttr());
                _reconsume = c;
                _currentState = RCDATAEndTagNameState;
                return null;
            default:
                _reconsume = c;
                _currentState = RCDATAState;
                return Emit('<', '/');
            }
        }

        private HtmlToken? RCDATAEndTagNameState() {
            int c = -1;

            HtmlToken anythingElse() {
                _reconsume = c;
                _currentState = RCDATAState;
                return Emit(true, '<', '/');
            }
            while (true) {
                c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    if (IsAppropriateEndTagToken()) {
                        _currentState = BeforeAttributeNameState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '/':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = SelfClosingStartTagState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '>':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = DataState;
                        return _currentToken;
                    } else {
                        return anythingElse();
                    }
                case int when IsUpperAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)( c + 0x20 ));
                    break;
                case int when IsLowerAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)c);
                    break;
                default:
                    return anythingElse();
                }
            }
        }

        private HtmlToken? RAWTEXTLessThanSignState() {
            int c = Advance();
            switch (c) {
            case '/':
                _tempBuffer = "";
                _currentState = RAWTEXTEndTagOpenState;
                return null;
            default:
                _reconsume = c;
                _currentState = RAWTEXTState;
                return Emit('<');
            }
        }

        private HtmlToken? RAWTEXTEndTagOpenState() {

            int c = Advance();
            switch (c) {
            case int when IsAlpha(c):
                _currentToken = new EndTagToken("", NewAttr());
                _reconsume = c;
                _currentState = RAWTEXTEndTagNameState;
                return null;
            default:
                return Emit('<', '/');
            }
        }

        private HtmlToken? RAWTEXTEndTagNameState() {
            int c = -1;
            HtmlToken anythingElse() {
                _reconsume = c;
                _currentState = RAWTEXTState;
                return Emit(true, '<', '/');
            }

            while (true) {
                c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    if (IsAppropriateEndTagToken()) {
                        _currentState = BeforeAttributeNameState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '/':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = SelfClosingStartTagState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '>':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = DataState;
                        return _currentToken;
                    } else {
                        return anythingElse();
                    }
                case int when IsUpperAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)( c + 0x20 ));
                    break;
                case int when IsLowerAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)c);
                    break;
                default:
                    return anythingElse();
                }
            }
        }

        private HtmlToken? ScriptDataLessThanSignState() {
            int c = Advance();

            switch (c) {
            case '/':
                _tempBuffer = "";
                _currentState = ScriptDataEndTagOpenState;
                return null;
            case '!':
                _currentState = ScriptDataEscapeStartState;
                return Emit('<', '!');
            default:
                _reconsume = c;
                _currentState = ScriptDataState;
                return Emit('<');
            }
        }

        private HtmlToken? ScriptDataEndTagOpenState() {
            int c = Advance();

            switch (c) {
            case int when IsAlpha(c):
                _currentToken = new EndTagToken("", NewAttr());
                _reconsume = c;
                _currentState = ScriptDataEndTagNameState;
                return null;
            default:
                _reconsume = c;
                _currentState = ScriptDataState;
                return Emit('<', '/');
            }
        }

        private HtmlToken? ScriptDataEndTagNameState() {

            int c = -1;


            HtmlToken anythingElse() {
                _reconsume = c;
                _currentState = ScriptDataState;
                return Emit(true, '<', '/');
            }

            while (true) {
                c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    if (IsAppropriateEndTagToken()) {
                        _currentState = BeforeAttributeNameState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '/':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = SelfClosingStartTagState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '>':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = DataState;
                        return _currentToken;
                    } else {
                        return anythingElse();
                    }
                case int when IsUpperAlpha(c):
                    _tempBuffer +=(char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)( c + 0x20 ));
                    break;
                case int when IsLowerAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)c);
                    break;
                default:
                    return anythingElse();
                }
            }
        }

        private HtmlToken? ScriptDataEscapeStartState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = ScriptDataEscapeStartDashState;
                return Emit('-');
            default:
                _reconsume = c;
                _currentState = ScriptDataState;
                return null;
            }
        }

        private HtmlToken? ScriptDataEscapeStartDashState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = ScriptDataEscapedDashDashState;
                return Emit('-');
            default:
                _reconsume = c;
                _currentState = ScriptDataState;
                return null;
            }
        }

        private HtmlToken? ScriptDataEscapedState() {
            int c = Advance();

            switch (c) {
            case '-':
                _currentState = ScriptDataEscapedDashState;
                return Emit('-');
            case '<':
                _currentState = ScriptDataEscapedLessThanSignState;
                return null;
            case '\0':
                Error("unexpecte-null-character");
                return Emit('\uFFFD');
            case EOF:
                Error("eof-in-script-html-comment-like-text");
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? ScriptDataEscapedDashState() {
            int c = Advance();

            switch (c) {
            case '-':
                _currentState = ScriptDataEscapedDashDashState;
                return Emit('-');
            case '<':
                _currentState = ScriptDataEscapedLessThanSignState;
                return null;
            case '\0':
                Error("unexpected-null-character");
                _currentState = ScriptDataEscapedState;
                return Emit('\uFFFD');
            case EOF:
                Error("eof-in-script-html-comment-line-text");
                return Emit(EOF);
            default:
                _currentState = ScriptDataEscapedState;
                return Emit(c);
            }

        }

        private HtmlToken? ScriptDataEscapedDashDashState() {
            int c = Advance();
            switch (c) {
            case '-':
                return Emit('-');
            case '<':
                _currentState = ScriptDataEscapedLessThanSignState;
                return null;
            case '>':
                _currentState = ScriptDataState;
                return Emit('>');
            case '\0':
                Error("unexpected-null-character");
                _currentState = ScriptDataEscapedState;
                return Emit('\uFFFD');
            case EOF:
                Error("eof-in-script-html-comment-like-text");
                return Emit(EOF);
            default:
                _currentState = ScriptDataEscapedState;
                return Emit(c);
            }
        }

        private HtmlToken? ScriptDataEscapedLessThanSignState() {
            int c = Advance();
            switch (c) {
            case '/':
                _tempBuffer = "";
                _currentState = ScriptDataEscapedEndTagOpenState;
                return null;
            case int when IsAlpha(c):
                _tempBuffer = "";
                _reconsume = c;
                _currentState = ScriptDataDoubleEscapeStartState;
                return Emit('<');
            default:
                _reconsume = c;
                _currentState = ScriptDataEscapedState;
                return Emit('<');
            }
        }

        private HtmlToken? ScriptDataEscapedEndTagOpenState() {
            int c = Advance();
            switch (c) {
            case int when IsAlpha(c):
                _currentToken = new EndTagToken("", NewAttr());
                _reconsume = c;
                _currentState = ScriptDataEscapedEndTagNameState;
                return null;
            default:
                _reconsume = c;
                _currentState = ScriptDataEscapedState;
                return Emit('<', '/');
            }
        }

        private HtmlToken? ScriptDataEscapedEndTagNameState() {
            int c = -1;
            HtmlToken anythingElse() {
                _reconsume = c;
                _currentState = ScriptDataEscapedState;
                return Emit(true, '<', '/');
            }

            while (true) {
                c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    if (IsAppropriateEndTagToken()) {
                        _currentState = BeforeAttributeNameState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '/':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = SelfClosingStartTagState;
                        return null;
                    } else {
                        return anythingElse();
                    }
                case '>':
                    if (IsAppropriateEndTagToken()) {
                        _currentState = DataState;
                        return _currentToken;
                    } else {
                        return anythingElse();
                    }
                case int when IsUpperAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)( c + 0x20 ));
                    break;
                case int when IsLowerAlpha(c):
                    _tempBuffer += (char)c;
                    ( _currentToken as TagToken )?.Name.Append((char)c);
                    break;
                default:
                    return anythingElse();
                }
            }
        }

        private HtmlToken? ScriptDataDoubleEscapeStartState() {
            int c = Advance();

            switch (c) {
            case int when IsMatchFor(c, '\t', '\n', '\r', ' ', '/', '>'):
                _currentState =_tempBuffer == "script" ?
                    ScriptDataDoubleEscapedState :
                    ScriptDataEscapedState;
                return Emit(c);
            case int when IsUpperAlpha(c):
                _tempBuffer += (char)( c + 0x20 );
                return Emit(c);
            case int when IsLowerAlpha(c):
                _tempBuffer += c;
                return Emit(c);
            default:
                _reconsume = c;
                _currentState = ScriptDataEscapedState;
                return null;
            }
        }

        private HtmlToken? ScriptDataDoubleEscapedState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = ScriptDataDoubleEscapedDashState;
                return Emit('-');
            case '<':
                _currentState = ScriptDataDoubleEscapedLessThanSignState;
                return Emit('<');
            case '\0':
                Error("unexpected-null-character");
                return Emit('\uFFFD');
            case EOF:
                Error("eof-in-script-html-comment-like-text");
                return Emit(EOF);
            default:
                return Emit(c);
            }
        }

        private HtmlToken? ScriptDataDoubleEscapedDashState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = ScriptDataDoubleEscapedDashDashState;
                return Emit('-');
            case '<':
                _currentState = ScriptDataDoubleEscapedLessThanSignState;
                return Emit('<');
            case '\0':
                Error("unexpected-null-character");
                _currentState = ScriptDataDoubleEscapedState;
                return Emit('\uFFFD');
            case EOF:
                Error("eof-in-scrip-html-comment-like-text");
                return Emit(EOF);
            default:
                _currentState = ScriptDataDoubleEscapedState;
                return Emit(c);
            }
        }

        private HtmlToken? ScriptDataDoubleEscapedDashDashState() {
            int c = Advance();

            switch (c) {
            case '-':
                return Emit('-');
            case '<':
                _currentState = ScriptDataDoubleEscapedLessThanSignState;
                return Emit('<');
            case '>':
                _currentState = ScriptDataState;
                return Emit('>');
            case '\0':
                Error("unescaped-null-character");
                _currentState = ScriptDataDoubleEscapedState;
                return Emit('\uFFFD');
            case EOF:
                Error("eof-in-script-html-comment-like-text");
                return Emit(EOF);
            default:
                _currentState = ScriptDataDoubleEscapedState;
                return Emit(c);
            }
        }

        private HtmlToken? ScriptDataDoubleEscapedLessThanSignState() {
            int c = Advance();
            switch (c) {
            case '/':
                _tempBuffer = "";
                _currentState = ScriptDataDoubleEscapeEndState;
                return Emit('/');
            default:
                _reconsume = c;
                _currentState = ScriptDataDoubleEscapedState;
                return null;
            }
        }

        private HtmlToken? ScriptDataDoubleEscapeEndState() {
            int c = Advance();

            switch (c) {
            case int when IsMatchFor(c, '\t', '\n', '\r', ' ', '/', '>'):
                _currentState = _tempBuffer == "script" ?
                    ScriptDataEscapedState :
                    ScriptDataDoubleEscapedState;
                return Emit(c);
            case int when IsUpperAlpha(c):
                _tempBuffer += (char)( c + 0x20 );
                return Emit(c);
            case int when IsLowerAlpha(c):
                _tempBuffer += (char)c;
                return Emit(c);
            default:
                _reconsume = c;
                _currentState = ScriptDataDoubleEscapedState;
                return null;
            }
        }

        private HtmlToken? BeforeAttributeNameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    // ignored
                    break;
                case int when IsMatchFor(c, '/', '>', EOF):
                    _reconsume = c;
                    _currentState = AfterAttributeNameState;
                    return null;
                case '=':
                    Error("unepected-equals-sign-before-attribute-name");
                    ( _currentToken as TagToken )
                        ?.StartAttr((char)c);
                    _currentState = AttributeNameState;
                    return null;
                default:
                    ( _currentToken as TagToken )
                        ?.StartAttr();
                    _reconsume = c;
                    _currentState = AttributeNameState;
                    return null;
                }
            }
        }

        private HtmlToken? AttributeNameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsMatchFor(c, '\t', '\n', '\r', ' ', '/', '>', EOF):
                    if (IsMatchFor(c, '/', '>', EOF)) {
                        ( _currentToken as TagToken )?.EndAttr();
                    }
                    _reconsume = c;
                    _currentState = AfterAttributeNameState;
                    return null;
                case '=':
                    _currentState = BeforeAttributeValueState;
                    return null;
                case int when IsUpperAlpha(c):
                    ( _currentToken as TagToken )
                        ?.AddToAttrName((char)( c + 0x20 ));
                    break;
                case '\0':
                    Error("unepected-null-character");
                    ( _currentToken as TagToken )
                        ?.AddToAttrName('\uFFFD');
                    break;
                case '"':
                case '\'':
                case '<':
                    Error("unexpected-character-in-attribute-name");
                    ( _currentToken as TagToken )
                        ?.AddToAttrName((char)c);
                    break;
                default:
                    ( _currentToken as TagToken )
                        ?.AddToAttrName((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AfterAttributeNameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    // ignore
                    break;
                case '/':
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState= SelfClosingStartTagState;
                    return null;
                case '=':
                    _currentState = BeforeAttributeValueState;
                    return null;
                case '>':
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-tag");
                    return Emit(EOF);
                default:
                    ( _currentToken as TagToken )
                        ?.StartAttr();
                    _reconsume = c;
                    _currentState = AttributeNameState;
                    return null;
                }
            }
        }

        private HtmlToken? BeforeAttributeValueState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    break;
                case '"':
                    _currentState = AttributeValueDoubleQuotedState;
                    return null;
                case '\'':
                    _currentState = AttributeValueSingleQuotedState;
                    return null;
                case '>':
                    Error("missing-attribute-value");
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState = DataState;
                    return _currentToken;
                default:
                    _reconsume = c;
                    _currentState = AttributeValueUnquotedState;
                    return null;
                }
            }
        }

        private HtmlToken? AttributeValueDoubleQuotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '"':
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState = AfterAttributeValueQuotedState;
                    return null;
                case '&':
                    _returnState = AttributeValueDoubleQuotedState;
                    _currentState = CharacterReferenceState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as TagToken )
                        ?.AddToAttrValue('\uFFFD');
                    break;
                case EOF:
                    Error("eof-in-tag");
                    return Emit(EOF);
                default:
                    ( _currentToken as TagToken )
                        ?.AddToAttrValue((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AttributeValueSingleQuotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '\'':
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState = AfterAttributeValueQuotedState;
                    return null;
                case '&':
                    _returnState = AttributeValueSingleQuotedState;
                    _currentState = CharacterReferenceState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as TagToken )
                        ?.AddToAttrValue('\uFFFD');
                    break;
                case EOF:
                    Error("eof-in-tag");
                    return Emit(EOF);
                default:
                    ( _currentToken as TagToken )
                        ?.AddToAttrValue((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AttributeValueUnquotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState = BeforeAttributeNameState;
                    return null;
                case '&':
                    _returnState = AttributeValueUnquotedState;
                    _currentState = CharacterReferenceState;
                    return null;
                case '>':
                    ( _currentToken as TagToken )?.EndAttr();
                    _currentState = DataState;
                    return _currentToken;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as TagToken )
                        ?.AddToAttrValue('\uFFFD');
                    break;
                case int when IsMatchFor(c, '"', '\'', '<', '=', '`'):
                    Error("unepxected-character-in-unquoted-attribute-value");
                    ( _currentToken as TagToken )?.AddToAttrValue((char)c);
                    break;
                case EOF:
                    Error("eof-in-tag");
                    return Emit(EOF);
                default:
                    ( _currentToken as TagToken )?.AddToAttrValue((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AfterAttributeValueQuotedState() {

            int c = Advance();
            switch (c) {
            case int when IsWhitespace(c):
                _currentState = BeforeAttributeNameState;
                return null;
            case '/':
                _currentState = SelfClosingStartTagState;
                return null;
            case '>':
                _currentState = DataState;
                return _currentToken;
            case EOF:
                Error("eof-in-tag");
                return Emit(EOF);
            default:
                Error("missing-whitespace-between-attributes");
                _reconsume = c;
                _currentState = BeforeAttributeNameState;
                return null;
            }
        }

        private HtmlToken? SelfClosingStartTagState() {
            int c = Advance();
            switch (c) {
            case '>':
                if (_currentToken is TagToken t) {
                    t.SelfClosing = true;
                }
                _currentState = DataState;
                return _currentToken;
            case EOF:
                Error("eof-in-tag");
                return Emit(EOF);
            default:
                Error("unexpected-solidus-in-tag");
                _reconsume = c;
                _currentState = BeforeAttributeNameState;
                return null;
            }
        }

        private HtmlToken? BogusCommentState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    return EmitEofAfter(_currentToken);
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as CommentToken )?.AddData('\uFFFD');
                    break;
                default:
                    ( _currentToken as CommentToken )?.AddData((char)c);
                    break;
                }
            }
        }

        private HtmlToken? MarkupDeclarationOpenState() {

            if (ExtendedMatch("--")) {
                _currentToken = new CommentToken("");
                _currentState = CommentStartState;
            } else if (ExtendedMatch("DOCTYPE")) {
                _currentState = DOCTYPEState;
            } else if (ExtendedMatch("[CDATA[", true)) {
                Error("cdata-in-html-content");
                _currentToken = new CommentToken("[CDATA[");
                _currentState = BogusCommentState;
            } else {
                Error("incorrectly-opened-comment");
                _currentToken = new CommentToken("");
                _currentState = BogusCommentState;
            }

            return null;
        }

        private HtmlToken? CommentStartState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = CommentStartDashState;
                return null;
            case '>':
                _currentState = DataState;
                return _currentToken;
            default:
                _reconsume = c;
                _currentState = CommentState;
                return null;
            }
        }

        private HtmlToken? CommentStartDashState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = CommentEndState;
                return null;
            case '>':
                Error("abrupt-closing-of-empty-comment");
                _currentState = DataState;
                return null;
            case EOF:
                Error("eof-in-comment");
                return EmitEofAfter(_currentToken);
            default:
                ( _currentToken as CommentToken )?.AddData('-');
                _reconsume = c;
                _currentState = CommentState;
                return null;
            }
        }

        private HtmlToken? CommentState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '<':
                    ( _currentToken as CommentToken )?.AddData('<');
                    _currentState = CommentLessThanSignState;
                    return null;
                case '-':
                    _currentState = CommentEndDashState;
                    return null;
                case '\0':
                    Error("unepxected-null-character");
                    ( _currentToken as CommentToken )?.AddData('\uFFFD');
                    break;
                case EOF:
                    Error("eof-in-comment");
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as CommentToken )?.AddData((char)c);
                    break;
                }
            }
        }

        private HtmlToken? CommentLessThanSignState() {
            int c = Advance();
            switch (c) {
            case '!':
                ( _currentToken as CommentToken )?.AddData('!');
                _currentState = CommentLessThanSignBangState;
                return null;
            case '<':
                ( _currentToken as CommentToken )?.AddData('<');
                return null;
            default:
                _reconsume = c;
                _currentState = CommentState;
                return null;
            }
        }

        private HtmlToken? CommentLessThanSignBangState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = CommentLessThanSignBangDashState;
                return null;
            default:
                _reconsume = c;
                _currentState = CommentState;
                return null;
            }
        }

        private HtmlToken? CommentLessThanSignBangDashState() {
            int c = Advance();
            switch (c) {
            case '-':
                _currentState = CommentLessThanSignBangDashDashState;
                return null;
            default:
                _reconsume = c;
                _currentState = CommentEndDashState;
                return null;
            }
        }

        private HtmlToken? CommentLessThanSignBangDashDashState() {
            int c = Advance();
            switch (c) {
            case '>':
            case EOF:
                _reconsume = c;
                _currentState = CommentEndState;
                return null;
            default:
                Error("nested-comment");
                _currentState = CommentEndState;
                return null;
            }
        }

        private HtmlToken? CommentEndDashState() {

            int c = Advance();
            switch (c) {
            case '-':
                _currentState = CommentEndState;
                return null;
            case EOF:
                Error("eof-in-comment");
                return EmitEofAfter(_currentToken);
            default:
                ( _currentToken as CommentToken )?.AddData('-');
                _reconsume = c;
                _currentState = CommentState;
                return null;
            }
        }

        private HtmlToken? CommentEndState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case '!':
                    _currentState = CommentEndBangState;
                    return null;
                case '-':
                    ( _currentToken as CommentToken )?.AddData('-');
                    break;
                case EOF:
                    Error("eof-in-comment");
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as CommentToken )?.AddData('-');
                    ( _currentToken as CommentToken )?.AddData('-');
                    _reconsume = c;
                    _currentState = CommentState;
                    return null;
                }
            }
        }

        private HtmlToken? CommentEndBangState() {
            int c = Advance();
            switch (c) {
            case '-':
                ( _currentToken as CommentToken )?.AddData('-');
                ( _currentToken as CommentToken )?.AddData('-');
                ( _currentToken as CommentToken )?.AddData('!');
                _currentState = CommentEndDashState;
                return null;
            case '>':
                Error("incorrectly-closed-comment");
                _currentState = DataState;
                return _currentToken;
            case EOF:
                Error("eof-in-comment");
                return EmitEofAfter(_currentToken);
            default:
                ( _currentToken as CommentToken )?.AddData('-');
                ( _currentToken as CommentToken )?.AddData('-');
                ( _currentToken as CommentToken )?.AddData('!');

                _reconsume = c;
                _currentState = CommentState;
                return null;
            }
        }

        private HtmlToken? DOCTYPEState() {
            int c = Advance();
            switch (c) {
            case int when IsWhitespace(c):
                _currentState = BeforeDOCTYPENameState;
                return null;
            case '>':
                _reconsume = c;
                _currentState = BeforeDOCTYPENameState;
                return null;
            case EOF:
                Error("eof-in-doctype");
                _currentToken = new DoctypeToken("", null, null, true);
                return EmitEofAfter(_currentToken);
            default:
                Error("missing-whitespace-before-doctype-name");
                _reconsume = c;
                _currentState = BeforeDOCTYPENameState;
                return null;
            }
        }

        private HtmlToken? BeforeDOCTYPENameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    // ignored
                    break;
                case int when IsUpperAlpha(c):
                    _currentToken = new DoctypeToken((char)( c + 0x20 ));
                    _currentState = DOCTYPENameState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    _currentToken = new DoctypeToken('\uFFFD');
                    _currentState = DOCTYPENameState;
                    return null;
                case EOF:
                    Error("eof-in-doctype");
                    _currentToken = new DoctypeToken("", null, null, true);
                    return EmitEofAfter(_currentToken);
                default:
                    _currentToken = new DoctypeToken((char)c);
                    _currentState = DOCTYPENameState;
                    return null;
                }
            }
        }

        private HtmlToken? DOCTYPENameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    _currentState = AfterDOCTYPENameState;
                    return null;
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case int when IsUpperAlpha(c):
                    ( _currentToken as DoctypeToken )?.AddName((char)( c + 0x20 ));
                    break;
                case '\0':
                    Error("unexected-null-character");
                    ( _currentToken as DoctypeToken )?.AddName('\uFFFD');
                    break;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as DoctypeToken )?.AddName((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AfterDOCTYPENameState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    // ignored
                    break;
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    if (ExtendedMatch("PUBLIC")) {
                        _currentState = AfterDOCTYPEPublicKeywordState;
                    } else if (ExtendedMatch("SYSTEM")) {
                        _currentState = AfterDOCTYPESystemKeywordState;
                    } else {
                        Error("invalid-character-sequence-after-doctype-name");
                        ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                        _reconsume = c;
                        _currentState = BogusDOCTYPEState;
                    }
                    return null;
                }
            }
        }

        private HtmlToken? AfterDOCTYPEPublicKeywordState() {
            int c = Advance();
            switch (c) {
            case int when IsWhitespace(c):
                _currentState = BeforeDOCTYPEPublicIdentifierState;
                return null;
            case '"':
                Error("missing-whitespace-after-doctype-public-keyword");
                ( _currentToken as DoctypeToken )?.SetPublic("");
                _currentState = DOCTYPEPublicIdentifierDoubleQuotedState;
                return null;
            case '\'':
                Error("missing-whitespace-after-doctype-public-keyword");
                ( _currentToken as DoctypeToken )?.SetPublic("");
                _currentState = DOCTYPEPublicIdentifierSingleQuotedState;
                return null;
            case '>':
                Error("missing-doctype-public-identifier");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                _currentState = DataState;
                return _currentToken;
            case EOF:
                Error("eof-in-doctype");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                return EmitEofAfter(_currentToken);
            default:
                Error("missing-quote-before-doctype-public-identifier");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                _reconsume = c;
                _currentState = BogusDOCTYPEState;
                return null;
            }
        }

        private HtmlToken? BeforeDOCTYPEPublicIdentifierState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    break;
                case '"':
                    ( _currentToken as DoctypeToken )?.SetPublic("");
                    _currentState = DOCTYPEPublicIdentifierDoubleQuotedState;
                    return null;
                case '\'':
                    ( _currentToken as DoctypeToken )?.SetPublic("");
                    _currentState = DOCTYPEPublicIdentifierSingleQuotedState;
                    return null;
                case '>':
                    Error("missing-doctype-public-identifier");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    Error("missing-quote-before-doctype-public-identifier");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _reconsume = c;
                    _currentState = BogusDOCTYPEState;
                    return null;
                }
            }
        }

        private HtmlToken? DOCTYPEPublicIdentifierDoubleQuotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '"':
                    _currentState = AfterDOCTYPEPublicIdentifierState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as DoctypeToken )?.AddPublic('\uFFFD');
                    break;
                case '>':
                    Error("abrupt-doctype-public-identifer");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as DoctypeToken )?.AddPublic((char)c);
                    break;
                }
            }
        }

        private HtmlToken? DOCTYPEPublicIdentifierSingleQuotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '\'':
                    _currentState = AfterDOCTYPEPublicIdentifierState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as DoctypeToken )?.AddPublic('\uFFFD');
                    break;
                case '>':
                    Error("abrupt-doctype-public-identifer");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as DoctypeToken )?.AddPublic((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AfterDOCTYPEPublicIdentifierState() {
            int c = Advance();
            switch (c) {
            case int when IsWhitespace(c):
                _currentState = BetweenDOCTYPEPublicAndSystemIdentifiersState;
                return null;
            case '>':
                _currentState = DataState;
                return _currentToken;
            case '"':
                Error("missing-whitespace-between-doctype-public-and-system-identifiers");
                ( _currentToken as DoctypeToken )?.SetSystem("");
                _currentState = DOCTYPESystemIdentifierDoubleQuotedState;
                return null;
            case '\'':
                Error("missing-whitespace-between-doctype-public-and-system-identifiers");
                ( _currentToken as DoctypeToken )?.SetSystem("");
                _currentState = DOCTYPESystemIdentifierSingleQuotedState;
                return null;

            case EOF:
                Error("eof-in-doctype");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                return EmitEofAfter(_currentToken);
            default:
                Error("missing-quote-before-doctype-system-identifier");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                _reconsume = c;
                _currentState = BogusDOCTYPEState;
                return null;
            }
        }

        private HtmlToken? BetweenDOCTYPEPublicAndSystemIdentifiersState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    // ignored
                    break;
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case '"':
                    ( _currentToken as DoctypeToken )?.SetSystem("");
                    _currentState = DOCTYPESystemIdentifierDoubleQuotedState;
                    return null;
                case '\'':
                    ( _currentToken as DoctypeToken )?.SetSystem("");
                    _currentState = DOCTYPESystemIdentifierSingleQuotedState;
                    return null;

                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    Error("missing-quote-before-doctype-system-identifier");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _reconsume = c;
                    _currentState = BogusDOCTYPEState;
                    return null;
                }
            }
        }

        private HtmlToken? AfterDOCTYPESystemKeywordState() {
            int c = Advance();
            switch (c) {
            case int when IsWhitespace(c):
                _currentState = BeforeDOCTYPESystemIdentifierState;
                return null;
            case '"':
                Error("missing-whitespace-after-doctype-system-keyword");
                ( _currentToken as DoctypeToken )?.SetPublic("");
                _currentState = DOCTYPESystemIdentifierDoubleQuotedState;
                return null;
            case '\'':
                Error("missing-whitespace-after-doctype-system-keyword");
                ( _currentToken as DoctypeToken )?.SetPublic("");
                _currentState = DOCTYPESystemIdentifierSingleQuotedState;
                return null;
            case '>':
                Error("missing-doctype-system-identifier");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                _currentState = DataState;
                return _currentToken;
            case EOF:
                Error("eof-in-doctype");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                return EmitEofAfter(_currentToken);
            default:
                Error("missing-quote-before-doctype-system-identifier");
                ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                _reconsume = c;
                _currentState = BogusDOCTYPEState;
                return null;
            }
        }

        private HtmlToken? BeforeDOCTYPESystemIdentifierState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    break;
                case '"':
                    ( _currentToken as DoctypeToken )?.SetSystem("");
                    _currentState = DOCTYPESystemIdentifierDoubleQuotedState;
                    return null;
                case '\'':
                    ( _currentToken as DoctypeToken )?.SetSystem("");
                    _currentState = DOCTYPESystemIdentifierSingleQuotedState;
                    return null;
                case '>':
                    Error("missing-doctype-system-identifier");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    Error("missing-quote-before-doctype-system-identifier");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _reconsume = c;
                    _currentState = BogusDOCTYPEState;
                    return null;
                }
            }
        }

        private HtmlToken? DOCTYPESystemIdentifierDoubleQuotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '"':
                    _currentState = AfterDOCTYPESystemIdentifierState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as DoctypeToken )?.AddSystem('\uFFFD');
                    break;
                case '>':
                    Error("abrupt-doctype-system-identifer");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as DoctypeToken )?.AddSystem((char)c);
                    break;
                }
            }
        }

        private HtmlToken? DOCTYPESystemIdentifierSingleQuotedState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '\'':
                    _currentState = AfterDOCTYPESystemIdentifierState;
                    return null;
                case '\0':
                    Error("unexpected-null-character");
                    ( _currentToken as DoctypeToken )?.AddSystem('\uFFFD');
                    break;
                case '>':
                    Error("abrupt-doctype-public-identifer");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    ( _currentToken as DoctypeToken )?.AddSystem((char)c);
                    break;
                }
            }
        }

        private HtmlToken? AfterDOCTYPESystemIdentifierState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when IsWhitespace(c):
                    // ignored
                    break;
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case EOF:
                    Error("eof-in-doctype");
                    ( _currentToken as DoctypeToken )?.SetForceQuirks(true);
                    return EmitEofAfter(_currentToken);
                default:
                    Error("missing-quote-after-doctype-system-identifier");
                    _reconsume = c;
                    _currentState = BogusDOCTYPEState;
                    return null;
                }
            }
        }

        private HtmlToken? BogusDOCTYPEState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case '>':
                    _currentState = DataState;
                    return _currentToken;
                case '\0':
                    Error("unexpected-null-character");
                    break;
                case EOF:
                    return EmitEofAfter(_currentToken);
                default:
                    // ignored
                    break;
                }
            }
        }

        private HtmlToken? CharacterReferenceState() {
            _tempBuffer = "&";
            int c = Advance();

            switch (c) {
            case int when IsAlphanumeric(c):
                _reconsume = c;
                _currentState = NamedCharacterReferenceState;
                return null;
            case '#':
                _tempBuffer += '#';
                _currentState = NumericCharacterReferenceState;
                return null;
            default:
                _reconsume = c;
                _currentState = _returnState ?? throw new NullReferenceException("_returnState unexpectedly null");
                return FlushCodePoints();
            }
        }

        private HtmlToken? NamedCharacterReferenceState() {

            PrefixTree.Node node = _namedEntities.Root;

            while (true) {
                int c = Peek();
                if (node.Children.ContainsKey((char)c)) {
                    node = node.Children[(char)c];
                    c = Advance();
                    _tempBuffer += (char)c;
                } else if (node.IsLeaf) {
                    // Found
                    if (_returnState == null || node.Value == null) {
                        throw new NullReferenceException("unexpected null in NCRS");
                    }
                    if (InAttribute && c != ';' && ( Peek() == '=' || IsAlphanumeric(Peek()) )) {
                        _currentState = _returnState;
                        return FlushCodePoints();
                    }
                    if (_tempBuffer[^1] != ';') {
                        Error("missing-semicolon-after-character-reference");
                    }
                    _tempBuffer = node.Value;
                    _currentState = _returnState;
                    return FlushCodePoints();
                } else {
                    // not found;
                    _currentState = AmbiguousAmpersandState;
                    return FlushCodePoints();
                }
            }
        }

        private HtmlToken? AmbiguousAmpersandState() {
            if (_returnState == null) { throw new NullReferenceException("Return state shouldn't be null"); }

            int c = Advance();
            switch (c) {
            case int when IsAlphanumeric(c):
                if (InAttribute) {
                    ( _currentToken as TagToken )?.AddToAttrValue((char)c);
                    return null;
                } else {
                    return Emit(c);
                }
            case ';':
                Error("unknown-named-character-reference");
                _reconsume = c;
                _currentState = _returnState;
                return null;
            default:
                _reconsume = c;
                _currentState = _returnState;
                return null;
            }
        }

        private HtmlToken? NumericCharacterReferenceState() {

            _characterReferenceCode =0;

            int c = Advance();
            switch (c) {
            case 'x':
            case 'X':
                _tempBuffer += (char)c;
                _currentState = HexadecimalCharacterReferenceStartState;
                return null;
            default:
                _reconsume = c;
                _currentState = DecimalCharacterReferenceStartState;
                return null;
            }
        }

        private HtmlToken? HexadecimalCharacterReferenceStartState() {
            if (_returnState == null) { throw new NullReferenceException("Return state shouldn't be null"); }
            int c = Advance();
            switch (c) {
            case int when IsHexDigit(c):
                _reconsume = c;
                _currentState = HexadecimalCharacterReferenceState;
                return null;
            default:
                Error("absence-of-digits-in-numeric-character-reference");
                _reconsume = c;
                _currentState = _returnState;
                return FlushCodePoints();
            }
        }

        private HtmlToken? DecimalCharacterReferenceStartState() {
            if (_returnState == null) { throw new NullReferenceException("Return state shouldn't be null"); }
            int c = Advance();
            switch (c) {
            case int when IsDigit(c):
                _reconsume = c;
                _currentState = DecimalCharacterReferenceState;
                return null;
            default:
                Error("absence-of-digits-in-numeric-character-reference");
                _reconsume = c;
                _currentState = _returnState;
                return FlushCodePoints();
            }
        }

        private HtmlToken? HexadecimalCharacterReferenceState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when c >= '0' && c <= '9':
                    _characterReferenceCode *= 16;
                    _characterReferenceCode += c - '0';
                    break;
                case int when c >= 'A' && c <= 'F':
                    _characterReferenceCode *= 16;
                    _characterReferenceCode += c - 'A';
                    break;
                case int when c >= 'a' && c <= 'f':
                    _characterReferenceCode *= 16;
                    _characterReferenceCode += c - 'a';
                    break;
                case ';':
                    _currentState = NumericCharacterReferenceEndState;
                    return null;
                default:
                    Error("missing-semicolon-after-character-reference");
                    _reconsume = c;
                    _currentState = NumericCharacterReferenceEndState;
                    return null;
                }
            }
        }

        private HtmlToken? DecimalCharacterReferenceState() {
            while (true) {
                int c = Advance();
                switch (c) {
                case int when c >= '0' && c <= '9':
                    _characterReferenceCode *= 10;
                    _characterReferenceCode += c - '0';
                    break;
                case ';':
                    _currentState = NumericCharacterReferenceEndState;
                    return null;
                default:
                    Error("missing-semicolon-after-character-reference");
                    _reconsume = c;
                    _currentState = NumericCharacterReferenceEndState;
                    return null;
                }
            }
        }

        private HtmlToken? NumericCharacterReferenceEndState() {

            if (_returnState == null) { throw new NullReferenceException("Return state shouldn't be null"); }
            if (_characterReferenceCode == 0) {
                Error("null-character-reference");
                _characterReferenceCode = 0xFFFD;
            } else if (_characterReferenceCode > 0x10FFF) {
                Error("character-reference-ouside-unicode-range");
                _characterReferenceCode = 0xFFFD;
            } else if (_characterReferenceCode >= 0xD800 && _characterReferenceCode <= 0xDFFF) {
                Error("surrogate-character-reference");
                _characterReferenceCode = 0xFFFD;
            } else if (_characterReferenceCode >= 0xFDD0 && _characterReferenceCode <= 0xFDFE
                || _characterReferenceCode == 0xFFFE || _characterReferenceCode == 0xFFFF
                || _characterReferenceCode == 0x1FFFE || _characterReferenceCode == 0x1FFFF
                || _characterReferenceCode == 0x2FFFE || _characterReferenceCode == 0x2FFFF
                || _characterReferenceCode == 0x3FFFE || _characterReferenceCode == 0x3FFFF
                || _characterReferenceCode == 0x4FFFE || _characterReferenceCode == 0x4FFFF
                || _characterReferenceCode == 0x5FFFE || _characterReferenceCode == 0x5FFFF
                || _characterReferenceCode == 0x6FFFE || _characterReferenceCode == 0x6FFFF
                || _characterReferenceCode == 0x7FFFE || _characterReferenceCode == 0x7FFFF
                || _characterReferenceCode == 0x8FFFE || _characterReferenceCode == 0x8FFFF
                || _characterReferenceCode == 0x9FFFE || _characterReferenceCode == 0x9FFFF
                || _characterReferenceCode == 0xaFFFE || _characterReferenceCode == 0xaFFFF
                || _characterReferenceCode == 0xbFFFE || _characterReferenceCode == 0xbFFFF
                || _characterReferenceCode == 0xcFFFE || _characterReferenceCode == 0xcFFFF
                || _characterReferenceCode == 0xdFFFE || _characterReferenceCode == 0xdFFFF
                || _characterReferenceCode == 0xeFFFE || _characterReferenceCode == 0xeFFFF
                || _characterReferenceCode == 0xfFFFE || _characterReferenceCode == 0xfFFFF
                || _characterReferenceCode == 0x10FFFE || _characterReferenceCode == 0x10FFFF) {
                Error("non-character-character-reference");
            } else if (( _characterReferenceCode >= 0 && _characterReferenceCode <= 0x1F
                ||  _characterReferenceCode >= 0x7F && _characterReferenceCode <= 0x9F )
                && !IsWhitespace(_characterReferenceCode)) {
                Error("control-character-reference");
                if (_controlCharacterReferenceReplcements.ContainsKey(_characterReferenceCode)) {
                    _characterReferenceCode = _controlCharacterReferenceReplcements[_characterReferenceCode];
                }
            }

            _tempBuffer =  char.ConvertFromUtf32(_characterReferenceCode);
            _currentState = _returnState;
            return FlushCodePoints();
        }

        private static IDictionary<string, string> NewAttr()
            => new Dictionary<string, string>();

        private bool IsAppropriateEndTagToken() =>
              _currentToken is EndTagToken end
           && _currentTag is StartTagToken start
           && end.Name == start.Name;

        private HtmlToken Emit(params int[] chars) => Emit(false, chars);

        private HtmlToken Emit(bool includeTemp, params int[] chars) {
            int[] toEmit;
            if (includeTemp) {
                toEmit = new int[chars.Length + _tempBuffer.Length];
                Array.Copy(_tempBuffer.ToCharArray(), toEmit, _tempBuffer.Length);
                Array.Copy(chars, 0, toEmit, _tempBuffer.Length, chars.Length);
            } else {
                toEmit = chars;
            }

            if (toEmit.Length > 1) {
                foreach (int c in toEmit[1..]) {
                    _tokenQueue.Enqueue(new CharacterToken((char)c));
                }
            }

            return toEmit[0] == EOF ? new EofToken() : new CharacterToken((char)toEmit[0]);
        }

        private HtmlToken? EmitEofAfter(HtmlToken? token) {
            _tokenQueue.Enqueue(new EofToken());
            return token;
        }

        private bool InAttribute => _returnState != null
            ? _returnState.Method.Name.StartsWith("Attribute")
            : throw new NullReferenceException("_returnState is null, shouldn't be posible");

        private HtmlToken? FlushCodePoints() {
            if (InAttribute) {
                ( _currentToken as TagToken )?.AddToAttrValue(_tempBuffer);
                return null;
            } else {
                return Emit(true);
            }
        }

        private static PrefixTree LoadNamedEntityReferences() {

            int count = 0;

            PrefixTree result = new();

            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string? baseFolder = Path.GetDirectoryName(assemblyPath);

            if (baseFolder != null) {
                try {
                    JObject json = JObject.Parse(File.ReadAllText(Path.Join(baseFolder, @"Data\entities.json")));

                    foreach (KeyValuePair<string, JToken?> pair in json) {
                        if (pair.Value != null) {
                            string entity = pair.Key[1..];
                            string? chars = (string?)pair.Value["characters"];

                            if (chars != null) {
                                result.Add(entity, chars);

                                count += 1;
                            }
                        }
                    }
                } catch (Exception ex) {
                    _log.Error(ex, "Can't load named entities: {exceptionMessage}", ex.Message);
                }
            }

            _log.Debug("Loaded {entityCount} named entities", count);

            return result;
        }

        private int Advance() {
            if (IsEnd()) {
                return EOF;
            }
            if (_reconsume != null) {
                char temp = (char)_reconsume;
                _reconsume = null;
                return temp;
            }
            int next = _text[_current++];
            _char += 1;
            if (next == '\n') {
                _line += 1;
                _char = 0;
            }

            return next;
        }

        private int Peek() {

            if (IsEnd()) {
                return EOF;
            }
            if (_reconsume != null) {
                return (int)_reconsume;
            }
            return _text[_current];
        }

        private bool IsEnd() => _reconsume == null &&  _current >= _text.Length;

        private bool ExtendedMatch(string text, bool caseSensitive = false) {

            if (_current + text.Length >= _text.Length) {
                return false;
            }
            string s = _text.Substring(_current, text.Length);

            if (caseSensitive && s == text  ||  !caseSensitive && s.ToLowerInvariant() == text.ToLowerInvariant()) {
                _current += text.Length;
                return true;
            }
            return false;
        }

        private void Error(string type) => ParseErrors.Add(new(type, _line, _char));

        private Func<HtmlToken?> _currentState;
        private Func<HtmlToken?>? _returnState;
        private HtmlToken? _currentTag;
        private HtmlToken? _currentToken;
        private const int EOF = -1;
        private int _char = 0;
        private int _current = 0;
        private int _line = 1;
        private int? _reconsume = null;
        private int _characterReferenceCode = -1;
        private readonly Queue<HtmlToken> _tokenQueue = new();
        private readonly static ILogger _log = Log.ForContext<HtmlTokeniser>();
        private readonly string _text;
        private static readonly PrefixTree _namedEntities;
        private string _tempBuffer = "";

        public IList<ParseError> ParseErrors { get; init; } = new List<ParseError>();

        private readonly static IDictionary<int, int> _controlCharacterReferenceReplcements = new Dictionary<int, int>() {
            { 0x80, 0x20AC }, { 0x82, 0x201A }, { 0x83, 0x0192 }, { 0x84, 0x201E }, { 0x85, 0x2026 }, { 0x86, 0x2020 },
            { 0x87, 0x2021 }, { 0x88, 0x02C6 }, { 0x89, 0x2030 }, { 0x8A, 0x0160 }, { 0x8B, 0x2039 }, { 0x8C, 0x0152 },
            { 0x8E, 0x017D }, { 0x91, 0x2018 }, { 0x92, 0x2019 }, { 0x93, 0x201C }, { 0x94, 0x201D }, { 0x95, 0x2022 },
            { 0x96, 0x2013 }, { 0x97, 0x2014 }, { 0x98, 0x02DC }, { 0x99, 0x2122 }, { 0x9A, 0x0161 }, { 0x9B, 0x203A },
            { 0x9C, 0x0153 }, { 0x9E, 0x017E }, { 0x9F, 0x0178 }
        };

        public record ParseError(string Name, int Line, int Char);
    }
}
