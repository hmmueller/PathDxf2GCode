namespace de.hmmueller.PathDxf2GCode {
    public class EmitGCodeException : Exception {
        public string ErrorContext { get; }

        public EmitGCodeException(string errorContext, string message) : base(message) {
            ErrorContext = errorContext;
        }

        public EmitGCodeException(string errorContext, string message, char key) : base(string.Format(message, key)) {
            ErrorContext = errorContext;
        }

    }
}