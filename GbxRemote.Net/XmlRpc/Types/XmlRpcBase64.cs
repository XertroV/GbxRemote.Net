﻿using GbxRemoteNet.XmlRpc.ExtraTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GbxRemoteNet.XmlRpc.Types {
    public class XmlRpcBase64 : XmlRpcBaseType, IEquatable<XmlRpcBase64> {
        public Base64 Value;

        public XmlRpcBase64(Base64 value) : base(null) {
            Value = value;
        }

        public XmlRpcBase64(XElement element) : base(element) {
            Value =  Base64.FromBase64String(element.Value);
        }

        public bool Equals(XmlRpcBase64 other) {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            return Equals((XmlRpcBase64)obj);
        }

        public override int GetHashCode() {
            return GetHashCode();
        }

        public override XElement GetXml() {
            return new XElement(XmlRpcElementNames.Base64, Value);
        }
    }
}
