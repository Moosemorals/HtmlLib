using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib {
    public abstract class HtmlToken : IEquatable<HtmlToken> {
        public HtmlTokenType Type { get; init; }
        public HtmlToken(HtmlTokenType type) {
            Type = type;
        }

        public override bool Equals(object? obj) => Equals(obj as HtmlToken);

        public bool Equals(HtmlToken? other) {
            return other != null && (
                ReferenceEquals(this, other)
                || GetType() == other.GetType()
                && Type == other.Type
            );
        }

        public override int GetHashCode() => Type.GetHashCode();
    }

    public class DoctypeToken : HtmlToken, IEquatable<DoctypeToken> {
        public string Name { get; set; }
        public string? Public { get; set; }
        public string? System { get; set; }
        public bool ForceQuirks { get; set; }

        public DoctypeToken(char name) : base(HtmlTokenType.Doctype) {
            Name = new string(name, 1);
            Public = null;
            System = null;
            ForceQuirks = false;
        }
        public DoctypeToken(string name, string? @public, string? system, bool forceQuirks = false) : base(HtmlTokenType.Doctype) {
            Name = name;
            Public = @public;
            System = system;
            ForceQuirks = forceQuirks;
        }

        public void SetForceQuirks(bool value) => ForceQuirks = value;

        public void AddName(char c) => Name += c;

        public void SetPublic(string s) => Public = s;
        public void AddPublic(char c) => Public += c;
        public void SetSystem(string s) => System = s;
        public void AddSystem(char c) => System += c;


        public override bool Equals(object? other) => Equals(other as DoctypeToken);

        public bool Equals(DoctypeToken? other) {
            if (other == null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }
            if (!base.Equals(other)) { return false; }

            return Name == other.Name
                && Public == other.Public
                && System == other.System
                && ForceQuirks == other.ForceQuirks
                ;
        }
        public override int GetHashCode() => (Name, Public, System, ForceQuirks).GetHashCode();
    }


    public abstract class TagToken : HtmlToken {
        public string Name { get; set; }
        public IDictionary<string, string> Attr { get; init; }
        public bool SelfClosing { get; set; }

        public TagToken(HtmlTokenType type, string name, IDictionary<string, string> attr, bool selfClosing = false) : base(type) {
            Name = name;
            Attr = attr;
            SelfClosing = selfClosing;

        }
        private string _attrName = "";
        private string _attrValue = "";

        public void StartAttr(char c) {
            _attrName = new string(c, 1);
            _attrValue = "";
        }

        public void StartAttr() {
            _attrName = "";
            _attrValue = "";
        }

        public void AddToName(char c) {
            Name += c;
        }

        public void AddToAttrName(char c) {
            _attrName += c;
        }

        public void AddToAttrValue(char c) {
            _attrValue += c;
        }

        public void AddToAttrValue(string s) {
            _attrValue += s;
        }
        public void EndAttr() {
            if (_attrName != "" && !Attr.ContainsKey(_attrName)) {
                Attr.Add(_attrName, _attrValue);
            }
        }
    }

    public class StartTagToken : TagToken, IEquatable<StartTagToken> {
        public StartTagToken(string Name, IDictionary<string, string> Attr, bool SelfClosing = false) : base(HtmlTokenType.StartTag, Name, Attr, SelfClosing) { }

        public override bool Equals(object? other) => Equals(other as StartTagToken);

        public bool Equals(StartTagToken? other) {
            if (other == null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }
            if (!base.Equals(other)) { return false; }

            return Name == other.Name
                && Attr.Count == other.Attr.Count && !Attr.Except(other.Attr).Any()
                && SelfClosing == other.SelfClosing;
        }

        public override int GetHashCode() => (Name, Attr, SelfClosing).GetHashCode();

        public override string ToString() => $"<{Name}>";
    }

    public class EndTagToken : TagToken, IEquatable<EndTagToken> {
        public EndTagToken(string Name, IDictionary<string, string> Attr, bool SelfClosing = false) : base(HtmlTokenType.StartTag, Name, Attr, SelfClosing) { }

        public override bool Equals(object? other) => Equals(other as EndTagToken);

        public bool Equals(EndTagToken? other) {
            if (other == null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }
            if (!base.Equals(other)) { return false; }

            return Name == other.Name
                && Attr.Count == other.Attr.Count && !Attr.Except(other.Attr).Any()
                && SelfClosing == other.SelfClosing;
        }

        public override int GetHashCode() => (Name, Attr, SelfClosing).GetHashCode();
        public override string ToString() => $"</{Name}>";
    }

    public class CommentToken : HtmlToken, IEquatable<CommentToken> {
        public string Data { get; set; }
        public CommentToken(string data) : base(HtmlTokenType.Comment) {
            Data = data;
        }

        public void AddData(char c) {
            Data += c;
        }
        public override bool Equals(object? other) => Equals(other as CommentToken);

        public bool Equals(CommentToken? other) {
            if (other == null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }
            if (!base.Equals(other)) { return false; }

            return Data == other.Data;
        }

        public override int GetHashCode() => Data.GetHashCode();
    }

    public class CharacterToken : HtmlToken, IEquatable<CharacterToken> {
        public char Char { get; set; }
        public CharacterToken(char c) : base(HtmlTokenType.Comment) {
            Char = c;
        }
        public override bool Equals(object? other) => Equals(other as CharacterToken);

        public bool Equals(CharacterToken? other) {
            if (other == null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }
            if (!base.Equals(other)) { return false; }

            return Char == other.Char;
        }

        public override int GetHashCode() => Char.GetHashCode();

        public override string ToString() => $"'{Char}'";

        public bool Is(params Predicate<int>[] tests) {
            foreach (Predicate<int> test in tests) {
                if (!test(Char)) {
                    return false;
                }
            }
            return true;
        }

    }
    public class EofToken : HtmlToken, IEquatable<EofToken> {
        public EofToken() : base(HtmlTokenType.EOF) { }
        public override bool Equals(object? other) => Equals(other as EofToken);

        public bool Equals(EofToken? other) {
            if (other == null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }
            if (!base.Equals(other)) { return false; }

            return true;
        }

        public override int GetHashCode() => base.GetHashCode();


    }

    public enum HtmlTokenType {
        Doctype,
        StartTag,
        EndTag,
        Comment,
        Character,
        EOF
    }

}
