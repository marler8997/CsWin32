using System;
using System.IO;
using System.Reflection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
            }
            this.unmanaged_export = (attrs & MethodAttributes.UnmanagedExport) != 0;
            this.is_static = (attrs & MethodAttributes.Static) != 0;
            this.is_final = (attrs & MethodAttributes.Final) != 0;
            this.is_virtual = (attrs & MethodAttributes.Virtual) != 0;
            this.hide_by_sig = (attrs & MethodAttributes.HideBySig) != 0;
            this.new_slot = (attrs & MethodAttributes.NewSlot) != 0;
            this.check_access_on_override = (attrs & MethodAttributes.CheckAccessOnOverride) != 0;
            this.is_abstract = (attrs & MethodAttributes.Abstract) != 0;
            this.special_name = (attrs & MethodAttributes.SpecialName) != 0;
            this.pinvoke_impl = (attrs & MethodAttributes.PinvokeImpl) != 0;
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

    // MethodImportAttribute Notes:
    //
    // Masks:
    //     b: BestFitMappingMask
    //     c: CallingConventionMask
    //     s: CharSetMask
    //     t: ThrowOnUnmappableCharMask
    //
    //     --tt -ccc --bb -ss-
    //
    // Flags:
    //
    //     --tt -ccc --bb -ss-
    //                |      |
    //                |      ExactSpelling
    //                |
    //                SetLastError
    //
    // BestFitMappingMask Enumeration:
    //
    //     #  | Name                  |
    //     ---| ----------------------|
    //     16 | BestFitMappingEnable  |
    //     32 | BestFitMappingDisable |
    //
    // CallingConventionMask Enumeration:
    //
    //     #    | Name                      |
    //     -----| --------------------------|
    //      256 | CallingConventionWinApi   |
    //      512 | CallingConventionCDecl    |
    //      768 | CallingConventionStdCall  |
    //     1024 | CallingConventionThisCall |
    //     1280 | CallingConventionFastCall |
    //
    // CharSetMask Enumeration:
    //
    //     # | Name           |
    //     --| ---------------|
    //     2 | CharSetAnsi    |
    //     4 | CharSetUnicode |
    //     6 | CharSetAuto    |
    //
    // ThrowOnUnmappableCharMask Enumeration:
    //
    //     #    | Name                         |
    //     -----| -----------------------------|
    //     4096 | ThrowOnUnmappableCharEnable  |
    //     8192 | ThrowOnUnmappableCharDisable |
    //
    struct DecodedMethodImportAttributes
    {
        public readonly bool exact_spelling;
        public readonly CharSet char_set;
        public readonly bool? best_fit;
        public readonly bool set_last_error;
        public readonly CallConv call_conv;
        public readonly bool? throw_on_unmappable_char;

        public DecodedMethodImportAttributes(MethodImportAttributes attrs)
        {
            this.exact_spelling = (attrs & MethodImportAttributes.ExactSpelling) != 0;
            {
                MethodImportAttributes char_set_attr = attrs & MethodImportAttributes.CharSetMask;
                this.char_set = char_set_attr switch
                {
                    MethodImportAttributes.None => CharSet.none,
                    MethodImportAttributes.CharSetAnsi => CharSet.ansi,
                    MethodImportAttributes.CharSetUnicode => CharSet.unicode,
                    MethodImportAttributes.CharSetAuto => CharSet.auto,
                    _ => throw new InvalidDataException(string.Format("unknown MethodImportAttributes char_set {0}", char_set_attr)),
                };
            }
            {
                MethodImportAttributes best_fit_attr = attrs & MethodImportAttributes.BestFitMappingMask;
                this.best_fit = best_fit_attr switch
                {
                    MethodImportAttributes.None => null,
                    MethodImportAttributes.BestFitMappingDisable => false,
                    MethodImportAttributes.BestFitMappingEnable => true,
                    _ => throw new InvalidDataException(string.Format("unknown MethodImportAttributes best_fit {0}", best_fit_attr)),
                };
            }
            this.set_last_error = (attrs & MethodImportAttributes.SetLastError) != 0;
            {
                MethodImportAttributes call_conv_attr = attrs & MethodImportAttributes.CallingConventionMask;
                this.call_conv = call_conv_attr switch
                {
                    MethodImportAttributes.CallingConventionWinApi => CallConv.winapi,
                    MethodImportAttributes.CallingConventionCDecl => CallConv.cdecl,
                    MethodImportAttributes.CallingConventionStdCall => CallConv.stdcall,
                    MethodImportAttributes.CallingConventionThisCall => CallConv.thiscall,
                    MethodImportAttributes.CallingConventionFastCall => CallConv.fastcall,
                    _ => throw new InvalidDataException(string.Format("unknown MethodImportAttributes call_conv {0}", call_conv_attr)),
                };
            }
            {
                MethodImportAttributes throw_attr = attrs & MethodImportAttributes.ThrowOnUnmappableCharMask;
                this.throw_on_unmappable_char = throw_attr switch
                {
                    MethodImportAttributes.None => null,
                    MethodImportAttributes.ThrowOnUnmappableCharDisable => false,
                    MethodImportAttributes.ThrowOnUnmappableCharEnable => true,
                    _ => throw new InvalidDataException(string.Format("unknown MethodImportAttributes throw_on_unmappable {0}", throw_attr)),
                };
            }
        }

        public override string ToString()
        {
            return string.Format(
                "CharSet={0}{1} SetLastError={2} BestFit={3} CallConv={4}{5}",
                this.char_set,
                this.exact_spelling ? " ExactSpelling" : "",
                this.set_last_error,
                this.best_fit is bool bb ? (bb ? "true" : "false") : "default",
                this.call_conv,
                this.throw_on_unmappable_char is bool tb ? (tb ? " ThrowOnUnmappableChar" : "") : "");
        }
    }

    public enum CharSet
    {
        none,
        ansi,
        unicode,
        auto,
    }

    public enum CallConv
    {
        winapi,
        cdecl,
        stdcall,
        thiscall,
        fastcall,
    }
}
