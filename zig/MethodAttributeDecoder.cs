using System;
using System.IO;
using System.Reflection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// MethodAttribute Notes:
//
// Masks:
//     m: MemberAccessMask
//     v: VtableLayoutMask
//     r: ReservedMask(runtime use only)
//
//     rr-r ---v ---- -mmm
//
// Flags:
//
//     RequireSecObject
//     | PinvokeImpl
//     | |  SpecialName
//     | |  | CheckAccessOnOverride
//     | |  | |  HideBySig
//     | |  | |  | Final
//     | |  | |  | | UnmanagedExport
//     | |  | |  | |  |
//     | |  | |  | |  |
//     rr-r ---v ---- -mmm
//      | |  |    | |
//      | |  |    | |
//      | |  |    | |
//      | |  |    | Static
//      | |  |    Virtual
//      | |  |
//      | |  Abstract
//      | RTSpecialName
//      HasSecurity
//
// The MemberAccessMask Enumeration:
//
//     # | Name         | Description
//     --| -------------| ---------
//     0 | PrivateScope | inaccessible
//     1 | Private      | accessible only by this type
//     2 | FamANDAssem  | accessible by this class and its derived classes but only in this assembly
//     3 | Assembly     | accessible to any class in this assembly
//     4 | Family       | accessible only to members of this class and its derived classes
//     5 | FamORAssem   | accessible to this class and its derived classes AND also any type in the assembly
//     6 | Public       | accessible to anyone
//
// The VtableLayoutMask Enumeration:
//
//     NOTE: Since there are only 2 values, I'm not sure why this was made into an enumeration
//           instead of just a flag like everything else.
//
//     #  | Name        | Description
//     ---| ------------| ---------
//      0 | ReuseSlot   | method will reuse an existing slot in the vtable (default)
//     256| NewSlot     | method always gets a new slot in the vtable
//
//
public static partial class ZigWin32
{
    public enum MemberAccess
    {
        private_scope,
        @private,
        family_and_assembly,
        assembly,
        family,
        family_or_assembly,
        @public,
    }

    struct DecodedMethodAttributes
    {
        public readonly MemberAccess member_access;
        public readonly bool unmanaged_export;
        public readonly bool is_static;
        public readonly bool is_final;
        public readonly bool is_virtual;
        public readonly bool hide_by_sig;
        public readonly bool new_slot;
        public readonly bool check_access_on_override;
        public readonly bool is_abstract;
        public readonly bool special_name;
        public readonly bool pinvoke_impl;

        public DecodedMethodAttributes(MethodAttributes attrs)
        {
            MethodAttributes member_access_attr = attrs & MethodAttributes.MemberAccessMask;
            this.member_access = member_access_attr switch
            {
                MethodAttributes.PrivateScope => MemberAccess.private_scope,
                MethodAttributes.Private => MemberAccess.@private,
                MethodAttributes.FamANDAssem => MemberAccess.family_and_assembly,
                MethodAttributes.Assembly => MemberAccess.assembly,
                MethodAttributes.Family => MemberAccess.family,
                MethodAttributes.FamORAssem => MemberAccess.family_or_assembly,
                MethodAttributes.Public => MemberAccess.@public,
                _ => throw new InvalidDataException(string.Format("unknown MethodAttributes member_access {0}", member_access_attr)),
            };
            this.unmanaged_export = (attrs & MethodAttributes.UnmanagedExport) != 0;
            this.is_static = (attrs & MethodAttributes.Static) != 0;
            this.is_final = (attrs & MethodAttributes.Final) != 0;
            this.is_virtual = (attrs & MethodAttributes.Virtual) != 0;
            this.hide_by_sig = (attrs & MethodAttributes.HideBySig) != 0;
            this.new_slot = (attrs & MethodAttributes.NewSlot) != 0;
            this.check_access_on_override = (attrs & MethodAttributes.CheckAccessOnOverride) != 0;
            this.is_abstract = (attrs & MethodAttributes.Abstract) != 0;
            this.special_name = (attrs & MethodAttributes.Abstract) != 0;
            this.pinvoke_impl = (attrs & MethodAttributes.Abstract) != 0;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}",
                this.member_access,
                this.is_static ? " static" : "",
                this.is_final ? " final" : "",
                this.is_virtual ? " virtual" : "",
                this.is_abstract ? " abstract" : "",
                this.pinvoke_impl ? " pinvoke" : "",
                this.hide_by_sig ? " HideBySig" : "",
                this.new_slot ? " NewSlot" : "",
                this.check_access_on_override ? " CheckOverrideAccess" : "",
                this.special_name ? " SpecialName" : "");
        }
    }
}
