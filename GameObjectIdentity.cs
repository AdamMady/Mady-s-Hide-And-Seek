// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;

namespace MadysHideNSeek;

// IL2CPP can return different managed wrappers for the same native object.
// Managed collections must compare native identity, not wrapper references.
sealed class NativeIdentity<T> : IEqualityComparer<T> where T : Il2CppObjectBase
{
    public static readonly NativeIdentity<T> Instance=new();
    public bool Equals(T left,T right)=>ReferenceEquals(left,right)||
        (!ReferenceEquals(left,null)&&!ReferenceEquals(right,null)&&left.Pointer==right.Pointer);
    public int GetHashCode(T value)=>ReferenceEquals(value,null)?0:value.Pointer.GetHashCode();
}

static class PropIdentity
{
    public static bool ContainsProp(this IEnumerable<Prop> values,Prop prop)
    {
        foreach(var value in values)if(NativeIdentity<Prop>.Instance.Equals(value,prop))return true;
        return false;
    }
}
