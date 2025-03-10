namespace de.hmmueller.PathDxf2GCode {
    public class EmitGCodeException : Exception {
        public string ErrorContext { get; }

        public EmitGCodeException(string errorContext, string message) : base(message) {
            ErrorContext = errorContext;
        }
    }
}