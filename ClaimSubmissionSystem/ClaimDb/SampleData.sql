USE ClaimSubmissionDB;
GO

-- Insert sample users (Password: Admin@123 hashed)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, PasswordHash, Email, FullName, IsActive)
    VALUES 
        ('admin', 'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', 'admin@claimsystem.com', 'Administrator', 1),
        ('claimmanager', 'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', 'manager@claimsystem.com', 'John Manager', 1);
END
GO

-- Insert sample claims
IF NOT EXISTS (SELECT 1 FROM Claims WHERE ClaimNumber = 'CLM-2026-001')
BEGIN
    INSERT INTO Claims (ClaimNumber, PatientName, ProviderName, DateOfService, ClaimAmount, ClaimStatus, CreatedBy)
    VALUES 
        ('CLM-2026-001', 'James Smith', 'Medical Services Inc.', '2026-02-15', 1500.00, 'Submitted', 1),
        ('CLM-2026-002', 'Mary Johnson', 'Healthcare Providers LLC', '2026-02-18', 2500.00, 'Under Review', 1),
        ('CLM-2026-003', 'Robert Davis', 'Clinical Services Corp', '2026-02-20', 3000.00, 'Approved', 1),
        ('CLM-2026-004', 'Patricia Williams', 'Medical Facilities', '2026-02-22', 1800.00, 'Rejected', 1),
        ('CLM-2026-005', 'Michael Brown', 'Healthcare Network', '2026-02-25', 2200.00, 'Submitted', 1);
END
GO

PRINT 'Sample data inserted successfully.';
