namespace BookSpot.Application.DTOs.Auth;

public sealed record ForgotPasswordSuccessResponse(string Message, bool Success);
public sealed record ResetPasswordSuccessResponse(string Message, bool Success);
public sealed record ResetTokenValidityResponse(bool Valid);
