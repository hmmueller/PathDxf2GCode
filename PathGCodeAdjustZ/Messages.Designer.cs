﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace de.hmmueller.PathGCodeAdjustZ {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PathGCodeAdjustZ.Messages", typeof(Messages).Assembly);
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
        ///   Sucht eine lokalisierte Zeichenfolge, die End of expression expected at column {0} ähnelt.
        /// </summary>
        internal static string ExprEval_EndOfExprExpected_Pos {
            get {
                return ResourceManager.GetString("ExprEval_EndOfExprExpected_Pos", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &apos;]&apos; expected at column {0} ähnelt.
        /// </summary>
        internal static string ExprEval_RBrcktExpected_Pos {
            get {
                return ResourceManager.GetString("ExprEval_RBrcktExpected_Pos", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die &apos;)&apos; expected at column {0} ähnelt.
        /// </summary>
        internal static string ExprEval_RParExpected_Pos {
            get {
                return ResourceManager.GetString("ExprEval_RParExpected_Pos", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Unexpected symbol &apos;{0}&apos; at column {1} ähnelt.
        /// </summary>
        internal static string ExprEval_Unexpected_Char_Pos {
            get {
                return ResourceManager.GetString("ExprEval_Unexpected_Char_Pos", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Call: PathGCodeAdjustZ [Options] [GCode files]
        ///
        ///Options:
        ///    /h     Help text
        ///    /l zzz Language
        /// ähnelt.
        /// </summary>
        internal static string Options_Help {
            get {
                return ResourceManager.GetString("Options_Help", resourceCulture);
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
        ///   Sucht eine lokalisierte Zeichenfolge, die Option {0} not supported ähnelt.
        /// </summary>
        internal static string Options_NotSupported_Name {
            get {
                return ResourceManager.GetString("Options_NotSupported_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Line does not have format &apos;(comment) #...=value&apos; ähnelt.
        /// </summary>
        internal static string Program_InvalidLineFormat {
            get {
                return ResourceManager.GetString("Program_InvalidLineFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Value &apos;{1}&apos; for {0} is not a valid number ähnelt.
        /// </summary>
        internal static string Program_NaN_Name_Value {
            get {
                return ResourceManager.GetString("Program_NaN_Name_Value", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die No GCode files specified ähnelt.
        /// </summary>
        internal static string Program_NoGCodeFiles {
            get {
                return ResourceManager.GetString("Program_NoGCodeFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Reading {0} ähnelt.
        /// </summary>
        internal static string Program_Reading_File {
            get {
                return ResourceManager.GetString("Program_Reading_File", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Writing {0} ähnelt.
        /// </summary>
        internal static string Program_Writing_File {
            get {
                return ResourceManager.GetString("Program_Writing_File", resourceCulture);
            }
        }
    }
}
