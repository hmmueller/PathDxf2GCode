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
    internal class Messages___Kopieren {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages___Kopieren() {
        }
        
        /// <summary>
        ///   Gibt die zwischengespeicherte ResourceManager-Instanz zurück, die von dieser Klasse verwendet wird.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PathDxf2GCode.Messages - Kopieren", typeof(Messages___Kopieren).Assembly);
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
        ///   Sucht eine lokalisierte Zeichenfolge, die Einlesen von {0} ähnelt.
        /// </summary>
        internal static string DxfHelper_ReadingFile__FileName {
            get {
                return ResourceManager.GetString("DxfHelper_ReadingFile__FileName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die ****  ähnelt.
        /// </summary>
        internal static string Error {
            get {
                return ResourceManager.GetString("Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die ----  ähnelt.
        /// </summary>
        internal static string Info {
            get {
                return ResourceManager.GetString("Info", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Aufruf: PathDxf2GCode [Parameter] [DXF-Dateien]
        ///
        ///Parameter:
        ///    /h     Hilfe-Anzeige
        ///    /f 000 Fräsgeschwindigkeit in mm/min; Pflichtwert
        ///    /v 000 Maximalgeschwindigkeit für Leerfahrten in mm/min; Pflichtwert
        ///    /c     Überprüfen aller Pfade in der DXF-Datei ohne G-Code-Ausgabe; wenn /c nicht 
        ///           angegeben wird, dann darf die DXF-Datei nur einen Pfad enthalten
        ///    /x zzz Gibt für alle auf diese Regex passenden Texte aus, welchem DXF-Objekt 
        ///           sie zugeordnet sind
        ///    /d zzz Suc [Rest der Zeichenfolge wurde abgeschnitten]&quot;; ähnelt.
        /// </summary>
        internal static string Options_Help {
            get {
                return ResourceManager.GetString("Options_Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Parameterwert {1} für {0} ist nicht &gt;= 0 ähnelt.
        /// </summary>
        internal static string Options_LessThan0_Name_Value {
            get {
                return ResourceManager.GetString("Options_LessThan0_Name_Value", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die /f nicht angegeben oder nicht größer als 0 ähnelt.
        /// </summary>
        internal static string Options_MissingF {
            get {
                return ResourceManager.GetString("Options_MissingF", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Fehlender Wert nach {0] ähnelt.
        /// </summary>
        internal static string Options_MissingOptionAfter_Name {
            get {
                return ResourceManager.GetString("Options_MissingOptionAfter_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die /v nicht angegeben oder nicht größer als 0 ähnelt.
        /// </summary>
        internal static string Options_MissingV {
            get {
                return ResourceManager.GetString("Options_MissingV", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Fehlender Parameterwert für {0} ähnelt.
        /// </summary>
        internal static string Options_MissingValue_Name {
            get {
                return ResourceManager.GetString("Options_MissingValue_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Parameterwert {1} für {0} ist keine Zahl ähnelt.
        /// </summary>
        internal static string Options_NaN_Name_Value {
            get {
                return ResourceManager.GetString("Options_NaN_Name_Value", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Option {0} nicht unterstützt ähnelt.
        /// </summary>
        internal static string Options_NotSupported_Name {
            get {
                return ResourceManager.GetString("Options_NotSupported_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die B={0} darf nicht über T={1} liegen ähnelt.
        /// </summary>
        internal static string Params_BMustBeLessThanT_B_T {
            get {
                return ResourceManager.GetString("Params_BMustBeLessThanT_B_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die C={0} muss &gt; 0 sein ähnelt.
        /// </summary>
        internal static string Params_CMustBeGtThan0_C {
            get {
                return ResourceManager.GetString("Params_CMustBeGtThan0_C", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die D={0} muss über B={1} liegen ähnelt.
        /// </summary>
        internal static string Params_DMustBeGtThanB_D_B {
            get {
                return ResourceManager.GetString("Params_DMustBeGtThanB_D_B", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die D={0} darf nicht über T={1} liegen ähnelt.
        /// </summary>
        internal static string Params_DMustBeLessThanT_D_T {
            get {
                return ResourceManager.GetString("Params_DMustBeLessThanT_D_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die F={0} muss &gt; 0 sein ähnelt.
        /// </summary>
        internal static string Params_FMustBeGtThan0_F {
            get {
                return ResourceManager.GetString("Params_FMustBeGtThan0_F", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I={0} muss &gt; 0 sein ähnelt.
        /// </summary>
        internal static string Params_IMustBeGtThan0_I {
            get {
                return ResourceManager.GetString("Params_IMustBeGtThan0_I", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0}-Wert fehlt ähnelt.
        /// </summary>
        internal static string Params_MissingKey_Key {
            get {
                return ResourceManager.GetString("Params_MissingKey_Key", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die O={0} muss &gt; 0 sein ähnelt.
        /// </summary>
        internal static string Params_OMustBeGtThan0_O {
            get {
                return ResourceManager.GetString("Params_OMustBeGtThan0_O", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die S={0} darf nicht unter oder nahe an T={1} liegen ähnelt.
        /// </summary>
        internal static string Params_SMustBeGtThanT_S_T {
            get {
                return ResourceManager.GetString("Params_SMustBeGtThanT_S_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die T={0} muss &gt; 0 sein ähnelt.
        /// </summary>
        internal static string Params_TMustBeGtThan0_T {
            get {
                return ResourceManager.GetString("Params_TMustBeGtThan0_T", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Nicht unterstützter {0}-Wert; {1} ähnelt.
        /// </summary>
        internal static string Params_UnsupportedKey_Name_Context {
            get {
                return ResourceManager.GetString("Params_UnsupportedKey_Name_Context", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die V={0} muss &gt; 0 sein ähnelt.
        /// </summary>
        internal static string Params_VMustBeGtThan0_V {
            get {
                return ResourceManager.GetString("Params_VMustBeGtThan0_V", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Durchmesser {0} kleiner als O-Wert {1} ähnelt.
        /// </summary>
        internal static string PathModel_CircleTooSmall_D_O {
            get {
                return ResourceManager.GetString("PathModel_CircleTooSmall_D_O", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Linienart {0} nicht unterstützt ähnelt.
        /// </summary>
        internal static string PathModel_LineTypeNotSupported_LineType {
            get {
                return ResourceManager.GetString("PathModel_LineTypeNotSupported_LineType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Ende-Markierung fehlt ähnelt.
        /// </summary>
        internal static string PathModel_MissingEnd {
            get {
                return ResourceManager.GetString("PathModel_MissingEnd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0}-Wert fehlt ähnelt.
        /// </summary>
        internal static string PathModel_MissingKey_Key {
            get {
                return ResourceManager.GetString("PathModel_MissingKey_Key", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Pfaddefinition {0} nicht gefunden ähnelt.
        /// </summary>
        internal static string PathModel_MissingPathDefinition_PathName {
            get {
                return ResourceManager.GetString("PathModel_MissingPathDefinition_PathName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Start-Markierung fehlt ähnelt.
        /// </summary>
        internal static string PathModel_MissingStart {
            get {
                return ResourceManager.GetString("PathModel_MissingStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Keine weiteren Segmente ab Punkt {0} gefunden ähnelt.
        /// </summary>
        internal static string PathModel_NoMoreSegmentsFound_P {
            get {
                return ResourceManager.GetString("PathModel_NoMoreSegmentsFound_P", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Kein überlappender Kreis, Bogen und keine überlappende Linie für &apos;{0}&apos; gefunden; evtl. Textmitte nicht nahe genug (Textkreis: {1}, Durchm. {2}) oder überlappender weiterer Text ähnelt.
        /// </summary>
        internal static string PathModel_NoObjectFound_Text_Center_Diameter {
            get {
                return ResourceManager.GetString("PathModel_NoObjectFound_Text_Center_Diameter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Kreis mit Linientyp PHANTOM (__ _ _ __) mit Durchmesser {0} hat keine spezielle Bedeutung ähnelt.
        /// </summary>
        internal static string PathModel_NotSpecialCircle_D {
            get {
                return ResourceManager.GetString("PathModel_NotSpecialCircle_D", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Objekt ...\n{0}\n...hat zugeordneten Text &apos;{1} ähnelt.
        /// </summary>
        internal static string PathModel_TextAssignment_Obj_Text {
            get {
                return ResourceManager.GetString("PathModel_TextAssignment_Obj_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Text-Layername &apos;{0}&apos; weicht von Element-Layername &apos;{1}&apos; ab ähnelt.
        /// </summary>
        internal static string PathModel_TextLayerDifferentFromElementLayer {
            get {
                return ResourceManager.GetString("PathModel_TextLayerDifferentFromElementLayer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Text {0} muss unrotiert mit Anker unten links oder oben links sein ähnelt.
        /// </summary>
        internal static string PathModel_TextLayout_Text {
            get {
                return ResourceManager.GetString("PathModel_TextLayout_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Zwei Endpunkte definiert: {0} und {1} ähnelt.
        /// </summary>
        internal static string PathModel_TwoEnds_E1_E2 {
            get {
                return ResourceManager.GetString("PathModel_TwoEnds_E1_E2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Zwei Anfangspunkte definiert: {0} und {1} ähnelt.
        /// </summary>
        internal static string PathModel_TwoStarts_S1_S2 {
            get {
                return ResourceManager.GetString("PathModel_TwoStarts_S1_S2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0} Segmente (u.a. dieses hier) wurden nicht erreicht - evtl. fehlt N-Auszeichnung ähnelt.
        /// </summary>
        internal static string PathModel_UnreachedSegments {
            get {
                return ResourceManager.GetString("PathModel_UnreachedSegments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Pfad {0} schon einmal definiert in {1} ähnelt.
        /// </summary>
        internal static string PathModelCollection_PathDefinedTwice_Path_File {
            get {
                return ResourceManager.GetString("PathModelCollection_PathDefinedTwice_Path_File", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die M={0} an Subpfad {1} weicht von M={2} in referenziertem Modell ab ähnelt.
        /// </summary>
        internal static string PathSegment_DifferingM_Caller_Path_Called {
            get {
                return ResourceManager.GetString("PathSegment_DifferingM_Caller_Path_Called", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die O={0} an Subpfad {1} weicht von O={2} in referenziertem Modell ab ähnelt.
        /// </summary>
        internal static string PathSegment_DifferingO_Caller_Path_Called {
            get {
                return ResourceManager.GetString("PathSegment_DifferingO_Caller_Path_Called", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Distanz {0} ist nicht gleich Distanz in Subpfad-Konstruktion {1} ähnelt.
        /// </summary>
        internal static string PathSegment_DistanceDiffers_CallerDist_CalledDist {
            get {
                return ResourceManager.GetString("PathSegment_DistanceDiffers_CallerDist_CalledDist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &gt;... oder &lt;... fehlt ähnelt.
        /// </summary>
        internal static string PathSegment_GtLtMissing {
            get {
                return ResourceManager.GetString("PathSegment_GtLtMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &apos;{0}{1}&apos; ist kein gültiger Pfadname ähnelt.
        /// </summary>
        internal static string PathSegment_InvalidPathName_Dir_Path {
            get {
                return ResourceManager.GetString("PathSegment_InvalidPathName_Dir_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &lt; noch nicht implementiert ähnelt.
        /// </summary>
        internal static string PathSegment_LtNotImplemented {
            get {
                return ResourceManager.GetString("PathSegment_LtNotImplemented", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Pfad {0} nicht in Datei(en) {1} gefunden ähnelt.
        /// </summary>
        internal static string PathSegment_PathNotFound_Name_Files {
            get {
                return ResourceManager.GetString("PathSegment_PathNotFound_Name_Files", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Überprüfung von {0} ähnelt.
        /// </summary>
        internal static string Program_Checking_Path {
            get {
                return ResourceManager.GetString("Program_Checking_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Befehlsanzahl ähnelt.
        /// </summary>
        internal static string Program_CommandCount {
            get {
                return ResourceManager.GetString("Program_CommandCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Bohrungen ähnelt.
        /// </summary>
        internal static string Program_DrillingLength {
            get {
                return ResourceManager.GetString("Program_DrillingLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Fräslänge ähnelt.
        /// </summary>
        internal static string Program_MillingLength {
            get {
                return ResourceManager.GetString("Program_MillingLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die DXF-Datei {0} enthält mehr als einen Pfad-LayerName: {1} ähnelt.
        /// </summary>
        internal static string Program_MoreThanOnePathLayer_File_Paths {
            get {
                return ResourceManager.GetString("Program_MoreThanOnePathLayer_File_Paths", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Keine DXF-Dateien angegeben ähnelt.
        /// </summary>
        internal static string Program_NoDxfFiles {
            get {
                return ResourceManager.GetString("Program_NoDxfFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die DXF-Datei {0} enthält keinen Pfad-LayerName ohne Fehler ähnelt.
        /// </summary>
        internal static string Program_NoErrorFreePathLayer_File {
            get {
                return ResourceManager.GetString("Program_NoErrorFreePathLayer_File", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Keine Segmente gefunden ähnelt.
        /// </summary>
        internal static string Program_NoSegmentsFound {
            get {
                return ResourceManager.GetString("Program_NoSegmentsFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Summe ähnelt.
        /// </summary>
        internal static string Program_SumLength {
            get {
                return ResourceManager.GetString("Program_SumLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Leerfahrten ähnelt.
        /// </summary>
        internal static string Program_SweepLength {
            get {
                return ResourceManager.GetString("Program_SweepLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Schreiben von {0} ähnelt.
        /// </summary>
        internal static string Program_Writing_Path {
            get {
                return ResourceManager.GetString("Program_Writing_Path", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Distanz {0}...{1} = {2} ist nicht gleich Distanz {3}...{4} = {5} ähnelt.
        /// </summary>
        internal static string Transformation2_DifferentDistances_FromS_FromE_FromD_ToS_ToE_ToD {
            get {
                return ResourceManager.GetString("Transformation2_DifferentDistances_FromS_FromE_FromD_ToS_ToE_ToD", resourceCulture);
            }
        }
    }
}
