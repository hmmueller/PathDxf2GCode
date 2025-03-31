﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace de.hmmueller.PathDxf2GCode {
    using System;
    
    
    /// <summary>
    ///   Eine stark typisierte Ressourcenklasse zum Suchen von lokalisierten Zeichenfolgen usw.
    /// </summary>
    // Diese Klasse wurde von der StronglyTypedResourceBuilder automatisch generiert
    // -Klasse über ein Tool wie ResGen oder Visual Studio automatisch generiert.
    // Um einen Member hinzuzufügen oder zu entfernen, bearbeiten Sie die .ResX-Datei und führen dann ResGen
    // mit der /str-Option erneut aus, oder Sie erstellen Ihr VS-Projekt neu.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Gibt die zwischengespeicherte ResourceManager-Instanz zurück, die von dieser Klasse verwendet wird.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PathDxf2GCode.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Überschreibt die CurrentUICulture-Eigenschaft des aktuellen Threads für alle
        ///   Ressourcenzuordnungen, die diese stark typisierte Ressourcenklasse verwenden.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Cannot load DXF file {0} ähnelt.
        /// </summary>
        internal static string DxfHelper_CannotLoadFile_Path {
            get {
                return ResourceManager.GetString("DxfHelper_CannotLoadFile_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Reading {0} ähnelt.
        /// </summary>
        internal static string DxfHelper_ReadingFile__FileName {
            get {
                return ResourceManager.GetString("DxfHelper_ReadingFile__FileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Call: PathDxf2GCode [options] [DXF files]
        ///
        ///Options:
        ///    /h     Help text
        ///    /f 000 Milling speed in mm/min; required
        ///    /v 000 Maximum speed for sweeps in mm/min; required
        ///    /c     Check all paths in DXF file without writing GCode; if /c is not
        ///           provided the DXF file must contain only one layer path
        ///    /x zzz For all texts matching this regular expression, write assigned
        ///           DXF objects; this is helpful for debugging parameter texts
        ///    /d zzz Search path for references DXF f [Rest der Zeichenfolge wurde abgeschnitten]&quot;; ähnelt.
        /// </summary>
        internal static string Options_Help {
            get {
                return ResourceManager.GetString("Options_Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Option value {1} for {0} ist not &gt;= 0 ähnelt.
        /// </summary>
        internal static string Options_LessThan0_Name_Value {
            get {
                return ResourceManager.GetString("Options_LessThan0_Name_Value", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die /f missing or not &gt; 0 ähnelt.
        /// </summary>
        internal static string Options_MissingF {
            get {
                return ResourceManager.GetString("Options_MissingF", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Missing value after option {0} ähnelt.
        /// </summary>
        internal static string Options_MissingOptionAfter_Name {
            get {
                return ResourceManager.GetString("Options_MissingOptionAfter_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die /v missing or not &gt; 0 ähnelt.
        /// </summary>
        internal static string Options_MissingV {
            get {
                return ResourceManager.GetString("Options_MissingV", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Missing value for option {0} ähnelt.
        /// </summary>
        internal static string Options_MissingValue_Name {
            get {
                return ResourceManager.GetString("Options_MissingValue_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Option value {1} for {0} is not a number ähnelt.
        /// </summary>
        internal static string Options_NaN_Name_Value {
            get {
                return ResourceManager.GetString("Options_NaN_Name_Value", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Option {0} not supported ähnelt.
        /// </summary>
        internal static string Options_NotSupported_Name {
            get {
                return ResourceManager.GetString("Options_NotSupported_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die B={0} must not be larger than T={1} ähnelt.
        /// </summary>
        internal static string Params_BMustBeLessThanT_B_T {
            get {
                return ResourceManager.GetString("Params_BMustBeLessThanT_B_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die C={0} must be &gt; 0 ähnelt.
        /// </summary>
        internal static string Params_CMustBeGtThan0_C {
            get {
                return ResourceManager.GetString("Params_CMustBeGtThan0_C", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die D={0} must be larger than B={1} ähnelt.
        /// </summary>
        internal static string Params_DMustBeGtThanB_D_B {
            get {
                return ResourceManager.GetString("Params_DMustBeGtThanB_D_B", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die D={0} must not be larger than T={1} ähnelt.
        /// </summary>
        internal static string Params_DMustBeLessThanT_D_T {
            get {
                return ResourceManager.GetString("Params_DMustBeLessThanT_D_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die F={0} must be &gt; 0 ähnelt.
        /// </summary>
        internal static string Params_FMustBeGtThan0_F {
            get {
                return ResourceManager.GetString("Params_FMustBeGtThan0_F", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I={0} must be &gt; 0 ähnelt.
        /// </summary>
        internal static string Params_IMustBeGtThan0_I {
            get {
                return ResourceManager.GetString("Params_IMustBeGtThan0_I", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0} value missing ähnelt.
        /// </summary>
        internal static string Params_MissingKey_Key {
            get {
                return ResourceManager.GetString("Params_MissingKey_Key", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die O={0} must be &gt; 0 ähnelt.
        /// </summary>
        internal static string Params_OMustBeGtThan0_O {
            get {
                return ResourceManager.GetString("Params_OMustBeGtThan0_O", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die S={0} must be above T={1}  ähnelt.
        /// </summary>
        internal static string Params_SMustBeGtThanT_S_T {
            get {
                return ResourceManager.GetString("Params_SMustBeGtThanT_S_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die T={0} must be &gt; 0 ähnelt.
        /// </summary>
        internal static string Params_TMustBeGtThan0_T {
            get {
                return ResourceManager.GetString("Params_TMustBeGtThan0_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Unsupported {0} value; {1} ähnelt.
        /// </summary>
        internal static string Params_UnsupportedKey_Name_Context {
            get {
                return ResourceManager.GetString("Params_UnsupportedKey_Name_Context", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die V={0} must be &gt; 0 ähnelt.
        /// </summary>
        internal static string Params_VMustBeGtThan0_V {
            get {
                return ResourceManager.GetString("Params_VMustBeGtThan0_V", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Diameter {0} is smaller than O={1} ähnelt.
        /// </summary>
        internal static string PathModel_CircleTooSmall_D_O {
            get {
                return ResourceManager.GetString("PathModel_CircleTooSmall_D_O", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Line type {0} not supported ähnelt.
        /// </summary>
        internal static string PathModel_LineTypeNotSupported_LineType {
            get {
                return ResourceManager.GetString("PathModel_LineTypeNotSupported_LineType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die End marker not near end of last traversed segment, but at {0} - add sweep ähnelt.
        /// </summary>
        internal static string PathModel_LostEnd_End {
            get {
                return ResourceManager.GetString("PathModel_LostEnd_End", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die End marker missing ähnelt.
        /// </summary>
        internal static string PathModel_MissingEnd {
            get {
                return ResourceManager.GetString("PathModel_MissingEnd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0} value missing ähnelt.
        /// </summary>
        internal static string PathModel_MissingKey_Key {
            get {
                return ResourceManager.GetString("PathModel_MissingKey_Key", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No parameters found for path {0}; maybe path text center is not near enough to start marker ähnelt.
        /// </summary>
        internal static string PathModel_MissingParams_Path {
            get {
                return ResourceManager.GetString("PathModel_MissingParams_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Path definition {0} not found; maybe start marker is not of line type PHANTOM(__ _ _ __) ähnelt.
        /// </summary>
        internal static string PathModel_MissingPathDefinition_PathName {
            get {
                return ResourceManager.GetString("PathModel_MissingPathDefinition_PathName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Start marker missing ähnelt.
        /// </summary>
        internal static string PathModel_MissingStart {
            get {
                return ResourceManager.GetString("PathModel_MissingStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No more segments found after location {0} ähnelt.
        /// </summary>
        internal static string PathModel_NoMoreSegmentsFound_P {
            get {
                return ResourceManager.GetString("PathModel_NoMoreSegmentsFound_P", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No overlapping circle, arc or line found for &apos;{0}&apos;; maybe text center is not near enough to element (text circle: {1}, diam. {2}), or there is another overlapping text ähnelt.
        /// </summary>
        internal static string PathModel_NoObjectFound_Text_Center_Diameter {
            get {
                return ResourceManager.GetString("PathModel_NoObjectFound_Text_Center_Diameter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Circle with line type PHANTOM (__ _ _ __) and diameter {0} has no special meaning ähnelt.
        /// </summary>
        internal static string PathModel_NotSpecialCircle_Diameter {
            get {
                return ResourceManager.GetString("PathModel_NotSpecialCircle_Diameter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Object
        ///   {0}
        ///has assigned text &apos;{1}&apos; ähnelt.
        /// </summary>
        internal static string PathModel_TextAssignment_Obj_Text {
            get {
                return ResourceManager.GetString("PathModel_TextAssignment_Obj_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Text layer name &apos;{0}&apos; different from element layer name &apos;{1}&apos; ähnelt.
        /// </summary>
        internal static string PathModel_TextLayerDifferentFromElementLayer_TextLayer_ElementLayer {
            get {
                return ResourceManager.GetString("PathModel_TextLayerDifferentFromElementLayer_TextLayer_ElementLayer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Text {0} must be unrotated, with anchor at bottom left or top left ähnelt.
        /// </summary>
        internal static string PathModel_TextLayout_Text {
            get {
                return ResourceManager.GetString("PathModel_TextLayout_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Two end markers defined at {0} and {1} ähnelt.
        /// </summary>
        internal static string PathModel_TwoEnds_E1_E2 {
            get {
                return ResourceManager.GetString("PathModel_TwoEnds_E1_E2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Two start markers defined at {0} and {1} ähnelt.
        /// </summary>
        internal static string PathModel_TwoStarts_S1_S2 {
            get {
                return ResourceManager.GetString("PathModel_TwoStarts_S1_S2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0} segments (e.g. this one here) not reached - maybe N value is missing ähnelt.
        /// </summary>
        internal static string PathModel_UnreachedSegments {
            get {
                return ResourceManager.GetString("PathModel_UnreachedSegments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Path {0} has already been defined in {1} ähnelt.
        /// </summary>
        internal static string PathModelCollection_PathDefinedTwice_Path_File {
            get {
                return ResourceManager.GetString("PathModelCollection_PathDefinedTwice_Path_File", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Nesting depth deeper than 9 levels at path {0} ähnelt.
        /// </summary>
        internal static string PathSegment_CallDepthGt9_Path {
            get {
                return ResourceManager.GetString("PathSegment_CallDepthGt9_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die M={0} at subpath {1} is different from M={2} in referenced path ähnelt.
        /// </summary>
        internal static string PathSegment_DifferingM_Caller_Path_Called {
            get {
                return ResourceManager.GetString("PathSegment_DifferingM_Caller_Path_Called", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die O={0} at subpath {1} is different from O={2} in referenced path ähnelt.
        /// </summary>
        internal static string PathSegment_DifferingO_Caller_Path_Called {
            get {
                return ResourceManager.GetString("PathSegment_DifferingO_Caller_Path_Called", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Distance {0} is not equal to distance {1} in referenced path ähnelt.
        /// </summary>
        internal static string PathSegment_DistanceDiffers_CallerDist_CalledDist {
            get {
                return ResourceManager.GetString("PathSegment_DistanceDiffers_CallerDist_CalledDist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &gt;... missing ähnelt.
        /// </summary>
        internal static string PathSegment_GtMissing {
            get {
                return ResourceManager.GetString("PathSegment_GtMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &apos;{0}{1}&apos; is not a valid path name ähnelt.
        /// </summary>
        internal static string PathSegment_InvalidPathName_Dir_Path {
            get {
                return ResourceManager.GetString("PathSegment_InvalidPathName_Dir_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Could not find path {0} in file(s) {1} ähnelt.
        /// </summary>
        internal static string PathSegment_PathNotFound_Name_Files {
            get {
                return ResourceManager.GetString("PathSegment_PathNotFound_Name_Files", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Checking {0} ähnelt.
        /// </summary>
        internal static string Program_Checking_Path {
            get {
                return ResourceManager.GetString("Program_Checking_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Command count: ähnelt.
        /// </summary>
        internal static string Program_CommandCount {
            get {
                return ResourceManager.GetString("Program_CommandCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Drill length: ähnelt.
        /// </summary>
        internal static string Program_DrillingLength {
            get {
                return ResourceManager.GetString("Program_DrillingLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Milling length: ähnelt.
        /// </summary>
        internal static string Program_MillingLength {
            get {
                return ResourceManager.GetString("Program_MillingLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die DXF file {0} contains more than one path layer: {1} ähnelt.
        /// </summary>
        internal static string Program_MoreThanOnePathLayer_File_Paths {
            get {
                return ResourceManager.GetString("Program_MoreThanOnePathLayer_File_Paths", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No dxf files provided ähnelt.
        /// </summary>
        internal static string Program_NoDxfFiles {
            get {
                return ResourceManager.GetString("Program_NoDxfFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die DXF file {0} contains no error-free path layer ähnelt.
        /// </summary>
        internal static string Program_NoErrorFreePathLayer_File {
            get {
                return ResourceManager.GetString("Program_NoErrorFreePathLayer_File", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No segments found ähnelt.
        /// </summary>
        internal static string Program_NoSegmentsFound {
            get {
                return ResourceManager.GetString("Program_NoSegmentsFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Sum: ähnelt.
        /// </summary>
        internal static string Program_SumLength {
            get {
                return ResourceManager.GetString("Program_SumLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Sweeps: ähnelt.
        /// </summary>
        internal static string Program_SweepLength {
            get {
                return ResourceManager.GetString("Program_SweepLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Writing {0} ähnelt.
        /// </summary>
        internal static string Program_Writing_Path {
            get {
                return ResourceManager.GetString("Program_Writing_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Distance {0}...{1} = {2} ist not equal to distance {3}...{4} = {5} ähnelt.
        /// </summary>
        internal static string Transformation2_DifferentDistances_FromS_FromE_FromD_ToS_ToE_ToD {
            get {
                return ResourceManager.GetString("Transformation2_DifferentDistances_FromS_FromE_FromD_ToS_ToE_ToD", resourceCulture);
            }
        }
    }
}
