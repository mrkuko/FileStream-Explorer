using System.Collections.Generic;
using System.Linq;

namespace FileStreamExplorer.Core.Models
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<string> Warnings { get; set; } = new List<string>();

        public static ValidationResult Valid()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Invalid(string error, ValidationErrorType type = ValidationErrorType.General)
        {
            var result = new ValidationResult { IsValid = false };
            result.Errors.Add(new ValidationError(error, type));
            return result;
        }

        public void AddError(string message, ValidationErrorType type = ValidationErrorType.General)
        {
            IsValid = false;
            Errors.Add(new ValidationError(message, type));
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        public void Merge(ValidationResult other)
        {
            if (!other.IsValid)
            {
                IsValid = false;
                Errors.AddRange(other.Errors);
            }
            Warnings.AddRange(other.Warnings);
        }

        public string GetErrorSummary()
        {
            return Errors.Any() 
                ? string.Join("\n", Errors.Select(e => e.Message))
                : string.Empty;
        }
    }

    public class ValidationError
    {
        public string Message { get; set; }
        public ValidationErrorType Type { get; set; }
        public string FilePath { get; set; }

        public ValidationError(string message, ValidationErrorType type = ValidationErrorType.General)
        {
            Message = message;
            Type = type;
        }

        public override string ToString() => $"[{Type}] {Message}";
    }

    public enum ValidationErrorType
    {
        General,
        InvalidPath,
        InvalidCharacters,
        PathTooLong,
        FileNotFound,
        AccessDenied,
        FileInUse,
        NameCollision,
        InvalidPattern
    }
}
