using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate.ErrorModel;

namespace MonkoraEdge.Core.Auth.Domain.Exceptions;

public class DomainException : Exception
{
    public string Module { get; }
    public ErrorCodeType ErrorCode { get; }
    public string ErrorMessage { get; }
    public string? CorrelationId { get; }
    public IList<ErrorField> Fields { get; private set; }
    public IList<ErrorRow> Rows { get; private set; }

    public DomainException(string module, ErrorCodeType errorCode, string errorMessage, string? correlationId = null)
        : base(errorMessage)
    {
        Module = module;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CorrelationId = correlationId;
        Fields = Array.Empty<ErrorField>();
        Rows = Array.Empty<ErrorRow>();
    }

    public DomainException(string module, ErrorCodeType errorCode, string? correlationId = null)
        : this(module, errorCode, errorCode.Description, correlationId)
    {
    }

    public DomainException(string module, string errorMessage, string? correlationId = null)
        : this(module, ErrorCodeType.VALIDATION_FAILED, errorMessage, correlationId)
    {
    }

    public DomainException(string module, IList<ErrorField> fields, string? correlationId = null)
        : this(module, ErrorCodeType.VALIDATION_FAILED, ErrorCodeType.VALIDATION_FAILED.Description, correlationId)
    {
        Fields = fields;
    }

    public DomainException(string module, IList<ErrorField> fields, IList<ErrorRow> rows, string? correlationId = null)
        : this(module, ErrorCodeType.VALIDATION_FAILED, ErrorCodeType.VALIDATION_FAILED.Description, correlationId)
    {
        Fields = fields;
        Rows = rows;
    }
}