namespace TrainingManagement.Api.Common.Responses;

/// <summary>单项校验错误，描述出错字段及原因。</summary>
public sealed record ApiError(string Field, string Message);
