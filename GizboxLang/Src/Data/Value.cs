﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Gizbox
{
    public enum GizType : byte
    {
        Void,
        Int,
        Float,
        Double,
        Bool,
        Char,
        String,
        GizObject,
        GizArray,
    }




    //Giz值类型    
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    public struct Value
    {
        // ---------- DATA --------------
        [FieldOffset(0)]
        private GizType type;

        [FieldOffset(4)]
        public bool AsBool;

        [FieldOffset(4)]
        public int AsInt;

        [FieldOffset(4)]
        public float AsFloat;

        [FieldOffset(4)]
        public double AsDouble;

        [FieldOffset(4)]
        public float AsChar;

        [FieldOffset(4)]
        public long AsPtr;




        // ---------- INTERFACE --------------

        public GizType Type => this.type;

        public bool IsVoid => (this.type == GizType.Void);

        public static Value Void => new Value() { type = GizType.Void };


        // ---------- ASSIGN --------------
        public static implicit operator Value(int v)
        {
            return new Value() { type = GizType.Int, AsInt = v };
        }
        public static implicit operator Value(float v)
        {
            return new Value() { type = GizType.Float, AsFloat = v };
        }
        public static implicit operator Value(double v)
        {
            return new Value() { type = GizType.Double, AsDouble = v };
        }
        public static implicit operator Value(bool v)
        {
            return new Value() { type = GizType.Bool, AsBool = v };
        }
        public static implicit operator Value(char v)
        {
            return new Value() { type = GizType.Char, AsChar = v };
        }


        public static Value FromConstStringPtr(int constDataPtr)
        {
            return new Value() { type = GizType.String, AsPtr = -constDataPtr };
        }
        public static Value FromStringPtr(long ptr)
        {
            return new Value() { type = GizType.String, AsPtr = ptr };
        }

        public static Value FromGizObjectPtr(long ptr)
        {
            return new Value() { type = GizType.GizObject, AsPtr = ptr };
        }

        public static Value FromArrayPtr(long arrayPtr)
        {
            return new Value() { type = GizType.GizArray, AsPtr = arrayPtr };
        }


        // ---------- OPERATOR --------------
        public static Value operator +(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误! " + v1.type + " + " + v2.type);
            switch (v1.type)
            {
                case GizType.Bool: throw new Exception("运算类型错误!");
                case GizType.Int: return v1.AsInt + v2.AsInt;
                case GizType.Float: return v1.AsFloat + v2.AsFloat;
                //case GizType.String: return (string)v1.AsObject + (string)v2.AsObject;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator -(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt - v2.AsInt;
                case GizType.Float: return v1.AsFloat - v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator *(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt * v2.AsInt;
                case GizType.Float: return v1.AsFloat * v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator /(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt / v2.AsInt;
                case GizType.Float: return v1.AsFloat / v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator %(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt % v2.AsInt;
                case GizType.Float: return v1.AsFloat % v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator >(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt > v2.AsInt;
                case GizType.Float: return v1.AsFloat > v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator <(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt < v2.AsInt;
                case GizType.Float: return v1.AsFloat < v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator <=(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt <= v2.AsInt;
                case GizType.Float: return v1.AsFloat <= v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator >=(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Int: return v1.AsInt >= v2.AsInt;
                case GizType.Float: return v1.AsFloat >= v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator ==(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Void: return v1.type == v2.type;
                case GizType.Bool: return v1.AsBool == v2.AsBool;
                case GizType.Int: return v1.AsInt == v2.AsInt;
                case GizType.Float: return v1.AsFloat == v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }
        public static Value operator !=(Value v1, Value v2)
        {
            if (v1.type != v2.type) throw new Exception("运算类型错误!");
            switch (v1.type)
            {
                case GizType.Bool: return v1.AsBool != v2.AsBool;
                case GizType.Int: return v1.AsInt != v2.AsInt;
                case GizType.Float: return v1.AsFloat != v2.AsFloat;
                default: throw new Exception("运算类型错误!");
            }
        }



        // ----------TO STRING ----------
        public override string ToString()
        {
            switch (this.type)
            {
                case GizType.Void: return "";
                case GizType.Bool: return AsBool.ToString();
                case GizType.Int: return AsInt.ToString();
                case GizType.Float: return AsFloat.ToString();
                case GizType.String:
                    {
                        return "GizType-String";
                    }
                case GizType.GizArray:
                    {
                        return "GizType-Array";
                    }
                case GizType.GizObject:
                    {
                        return "GizType-Object";
                    }
                default:
                    {
                        return "???";
                    };
            }
        }
    }

    // Giz对象  
    public class GizObject
    {
        private static int currentMaxId = 0;

        // ***** Header *****
        public int instanceID = 0;
        public string truetype;
        public SymbolTable classEnv;
        public VTable vtable;

        // ***** Data *****
        public Dictionary<string, Value> fields = new Dictionary<string, Value>();

        public GizObject(string classname, ScriptEngine.ScriptEngine engineContext)
        {
            this.instanceID = currentMaxId++;
            this.truetype = classname;
            this.classEnv = engineContext.mainUnit.QueryClass(classname).envPtr;
            this.vtable = engineContext.mainUnit.vtables[classname];
        }
        public GizObject(string classname, SymbolTable classEnv, VTable vtable)
        {
            this.instanceID = currentMaxId++;
            this.truetype = classname;
            this.classEnv = classEnv;
            this.vtable = vtable;
        }
        public override string ToString()
        {
            return "GizObect(instanceID:" + this.instanceID + ")";
        }
    }




}
