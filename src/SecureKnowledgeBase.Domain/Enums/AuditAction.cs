namespace SecureKnowledgeBase.Domain.Enums;

public enum AuditAction
{
    Login = 1,
    Logout = 2,
    DocumentUpload = 3,
    DocumentDelete = 4,
    DocumentUpdate = 5,
    FileDownload = 6,
    AssistantQuery = 7,
    AccessDenied = 8,
    AdminAction = 9,
    SecurityEvent = 10
}
