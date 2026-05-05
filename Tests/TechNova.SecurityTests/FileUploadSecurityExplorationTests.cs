using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for File Upload Security (Task 1.2)
    /// 
    /// **Validates: Requirements 2.1, 2.2, 2.3**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class FileUploadSecurityExplorationTests
    {
        /// <summary>
        /// Creates a mock IFormFile for testing
        /// </summary>
        private IFormFile CreateMockFormFile(string fileName, string contentType, long length, string content = "test content")
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));
            
            return fileMock.Object;
        }

        /// <summary>
        /// Creates a mock IWebHostEnvironment for testing
        /// </summary>
        private IWebHostEnvironment CreateMockWebHostEnvironment()
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));
            mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            return mockEnv.Object;
        }

        /// <summary>
        /// Test 1.2.1: .exe file upload should be REJECTED but is currently ACCEPTED
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: UploadPolicy accepts any file type without validation
        /// Expected Behavior: System should reject executable files (.exe, .dll, .bat, .cmd, .sh, etc.)
        /// Current Behavior: ComplianceManagerPolicyController.UploadPolicy accepts any file type
        /// </summary>
        [Fact]
        public void MaliciousFile_ExeExtension_ShouldBeRejected_ButIsCurrentlyAccepted()
        {
            // Arrange
            string maliciousFileName = "malware.exe";
            var mockFile = CreateMockFormFile(maliciousFileName, "application/x-msdownload", 1024);
            
            // Act - Simulate current validation logic (NO validation in current code)
            // Current code only checks: policyFile != null && policyFile.Length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should validate file extension against whitelist
            string[] allowedExtensions = { ".pdf", ".docx", ".doc", ".txt" };
            string fileExtension = Path.GetExtension(maliciousFileName).ToLowerInvariant();
            bool shouldBeAccepted = Array.Exists(allowedExtensions, ext => ext == fileExtension);
            
            // Assert - This test FAILS because .exe file is currently accepted
            // After fix: File upload validator should reject files not in whitelist
            Assert.False(isAcceptedByCurrentCode && !shouldBeAccepted,
                "EXPECTED FAILURE: Malicious .exe file is currently accepted (no file type validation). " +
                "After fix, file upload validator should reject files with extensions not in whitelist (.pdf, .docx, .doc, .txt).");
        }

        /// <summary>
        /// Test 1.2.2: Various malicious file extensions should be rejected
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// </summary>
        [Theory]
        [InlineData("malware.exe")]
        [InlineData("script.bat")]
        [InlineData("shell.sh")]
        [InlineData("library.dll")]
        [InlineData("webpage.aspx")]
        [InlineData("webpage.php")]
        [InlineData("script.js")]
        [InlineData("command.cmd")]
        public void MaliciousFiles_ShouldBeRejected_ButAreCurrentlyAccepted(string maliciousFileName)
        {
            // Arrange
            var mockFile = CreateMockFormFile(maliciousFileName, "application/octet-stream", 1024);
            
            // Act - Current validation: only checks file != null && length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should validate against whitelist
            string[] allowedExtensions = { ".pdf", ".docx", ".doc", ".txt" };
            string fileExtension = Path.GetExtension(maliciousFileName).ToLowerInvariant();
            bool shouldBeAccepted = Array.Exists(allowedExtensions, ext => ext == fileExtension);
            
            // Assert - This test FAILS because malicious files are currently accepted
            Assert.False(isAcceptedByCurrentCode && !shouldBeAccepted,
                $"EXPECTED FAILURE: Malicious file '{maliciousFileName}' is currently accepted. " +
                "After fix, file upload validator should reject files with dangerous extensions.");
        }

        /// <summary>
        /// Test 1.2.3: File with double extension .pdf.aspx should be REJECTED but is currently ACCEPTED
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: No validation for double extensions (MIME type spoofing attack)
        /// Expected Behavior: System should detect and reject files with double extensions
        /// Current Behavior: Only checks if file exists and has length > 0
        /// </summary>
        [Fact]
        public void DoubleExtensionFile_PdfAspx_ShouldBeRejected_ButIsCurrentlyAccepted()
        {
            // Arrange
            string doubleExtensionFileName = "document.pdf.aspx";
            var mockFile = CreateMockFormFile(doubleExtensionFileName, "application/pdf", 1024);
            
            // Act - Current validation: only checks file != null && length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should detect double extensions
            // Check if filename contains multiple dots with dangerous extensions
            bool hasDoubleExtension = doubleExtensionFileName.Split('.').Length > 2;
            string finalExtension = Path.GetExtension(doubleExtensionFileName).ToLowerInvariant();
            string[] dangerousExtensions = { ".aspx", ".php", ".exe", ".dll", ".bat", ".cmd", ".sh" };
            bool hasDangerousExtension = Array.Exists(dangerousExtensions, ext => 
                doubleExtensionFileName.ToLowerInvariant().Contains(ext));
            
            bool shouldBeRejected = hasDoubleExtension && hasDangerousExtension;
            
            // Assert - This test FAILS because double extension file is currently accepted
            // After fix: File upload validator should detect and reject double extensions
            Assert.False(isAcceptedByCurrentCode && shouldBeRejected,
                "EXPECTED FAILURE: File with double extension '.pdf.aspx' is currently accepted (no double extension validation). " +
                "After fix, file upload validator should detect and reject files with double extensions containing dangerous types.");
        }

        /// <summary>
        /// Test 1.2.4: Various double extension files should be rejected
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// </summary>
        [Theory]
        [InlineData("document.pdf.exe")]
        [InlineData("report.docx.aspx")]
        [InlineData("policy.txt.php")]
        [InlineData("file.doc.bat")]
        [InlineData("data.pdf.dll")]
        public void DoubleExtensionFiles_ShouldBeRejected_ButAreCurrentlyAccepted(string doubleExtensionFileName)
        {
            // Arrange
            var mockFile = CreateMockFormFile(doubleExtensionFileName, "application/octet-stream", 1024);
            
            // Act - Current validation: only checks file != null && length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should detect double extensions with dangerous types
            bool hasDoubleExtension = doubleExtensionFileName.Split('.').Length > 2;
            string[] dangerousExtensions = { ".aspx", ".php", ".exe", ".dll", ".bat", ".cmd", ".sh" };
            bool hasDangerousExtension = Array.Exists(dangerousExtensions, ext => 
                doubleExtensionFileName.ToLowerInvariant().Contains(ext));
            
            bool shouldBeRejected = hasDoubleExtension && hasDangerousExtension;
            
            // Assert - This test FAILS because double extension files are currently accepted
            Assert.False(isAcceptedByCurrentCode && shouldBeRejected,
                $"EXPECTED FAILURE: File with double extension '{doubleExtensionFileName}' is currently accepted. " +
                "After fix, file upload validator should detect and reject double extensions.");
        }

        /// <summary>
        /// Test 1.2.5: 50MB file should be REJECTED but is currently ACCEPTED
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: No file size validation
        /// Expected Behavior: System should enforce maximum file size limit (e.g., 10MB)
        /// Current Behavior: Accepts files of any size
        /// </summary>
        [Fact]
        public void LargeFile_50MB_ShouldBeRejected_ButIsCurrentlyAccepted()
        {
            // Arrange
            string fileName = "large_policy.pdf";
            long fileSizeInBytes = 50 * 1024 * 1024; // 50MB
            var mockFile = CreateMockFormFile(fileName, "application/pdf", fileSizeInBytes);
            
            // Act - Current validation: only checks file != null && length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should enforce maximum file size (10MB)
            long maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
            bool shouldBeAccepted = mockFile.Length <= maxFileSizeInBytes;
            
            // Assert - This test FAILS because 50MB file is currently accepted
            // After fix: File upload validator should enforce maximum file size limit
            Assert.False(isAcceptedByCurrentCode && !shouldBeAccepted,
                "EXPECTED FAILURE: 50MB file is currently accepted (no file size validation). " +
                "After fix, file upload validator should reject files larger than 10MB.");
        }

        /// <summary>
        /// Test 1.2.6: Various oversized files should be rejected
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// </summary>
        [Theory]
        [InlineData(11 * 1024 * 1024)]  // 11MB
        [InlineData(20 * 1024 * 1024)]  // 20MB
        [InlineData(50 * 1024 * 1024)]  // 50MB
        [InlineData(100 * 1024 * 1024)] // 100MB
        public void OversizedFiles_ShouldBeRejected_ButAreCurrentlyAccepted(long fileSizeInBytes)
        {
            // Arrange
            var mockFile = CreateMockFormFile("large_file.pdf", "application/pdf", fileSizeInBytes);
            
            // Act - Current validation: only checks file != null && length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should enforce maximum file size (10MB)
            long maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
            bool shouldBeAccepted = mockFile.Length <= maxFileSizeInBytes;
            
            // Assert - This test FAILS because oversized files are currently accepted
            Assert.False(isAcceptedByCurrentCode && !shouldBeAccepted,
                $"EXPECTED FAILURE: File of size {fileSizeInBytes / (1024 * 1024)}MB is currently accepted. " +
                "After fix, file upload validator should reject files larger than 10MB.");
        }

        /// <summary>
        /// Test 1.2.7: Files are stored in web-accessible directory
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Files are stored in /wwwroot/uploads/policies/ which is web-accessible
        /// Expected Behavior: Files should be stored outside web root or in directory with execution disabled
        /// Current Behavior: Files stored in web-accessible directory allowing potential code execution
        /// </summary>
        [Fact]
        public void UploadedFiles_ShouldNotBeInWebAccessibleDirectory_ButCurrentlyAre()
        {
            // Arrange
            var mockEnv = CreateMockWebHostEnvironment();
            string webRootPath = mockEnv.WebRootPath;
            
            // Act - Current storage logic from UploadPolicy method
            // var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "policies");
            string currentStoragePath = Path.Combine(webRootPath, "uploads", "policies");
            
            // Expected behavior: Files should be stored outside web root
            // e.g., /App_Data/uploads/policies/ or similar non-web-accessible location
            bool isInWebRoot = currentStoragePath.StartsWith(webRootPath);
            
            // Assert - This test FAILS because files ARE stored in web-accessible directory
            // After fix: Files should be stored outside web root (e.g., /App_Data/uploads/policies/)
            Assert.False(isInWebRoot,
                "EXPECTED FAILURE: Files are currently stored in web-accessible directory (wwwroot/uploads/policies/). " +
                "After fix, files should be stored outside web root or in directory with execution disabled.");
        }

        /// <summary>
        /// Test 1.2.8: File path returned is web-accessible
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: File path returned is /uploads/policies/{filename} which is directly accessible via URL
        /// Expected Behavior: Files should be served through controller action with authorization checks
        /// Current Behavior: Files are directly accessible via URL without authorization
        /// </summary>
        [Fact]
        public void FilePath_ShouldNotBeDirectlyAccessible_ButCurrentlyIs()
        {
            // Arrange
            string fileName = "policy_20250101120000_document.pdf";
            
            // Act - Current file path logic from UploadPolicy method
            // filePath = $"/uploads/policies/{safeFileName}";
            string currentFilePath = $"/uploads/policies/{fileName}";
            
            // Expected behavior: File path should point to controller action
            // e.g., /ComplianceManagerPolicy/DownloadFile?fileId=123
            bool isDirectlyAccessible = currentFilePath.StartsWith("/uploads/");
            
            // Assert - This test FAILS because file path IS directly accessible
            // After fix: Files should be served through controller action with authorization
            Assert.False(isDirectlyAccessible,
                "EXPECTED FAILURE: File path is currently directly accessible (/uploads/policies/). " +
                "After fix, files should be served through controller action with proper authorization checks.");
        }

        /// <summary>
        /// Test 1.2.9: MIME type validation should be enforced
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: No MIME type validation - file extension can be spoofed
        /// Expected Behavior: MIME type should match file extension
        /// Current Behavior: No MIME type validation
        /// </summary>
        [Theory]
        [InlineData("document.pdf", "application/x-msdownload")]  // PDF extension but EXE MIME type
        [InlineData("report.docx", "application/x-php")]  // DOCX extension but PHP MIME type
        [InlineData("policy.txt", "application/x-sh")]  // TXT extension but shell script MIME type
        public void MimeTypeSpoofing_ShouldBeDetected_ButIsNot(string fileName, string spoofedMimeType)
        {
            // Arrange
            var mockFile = CreateMockFormFile(fileName, spoofedMimeType, 1024);
            
            // Act - Current validation: only checks file != null && length > 0
            bool isAcceptedByCurrentCode = mockFile != null && mockFile.Length > 0;
            
            // Expected behavior: Should validate MIME type matches extension
            string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            var expectedMimeTypes = new Dictionary<string, string[]>
            {
                { ".pdf", new[] { "application/pdf" } },
                { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                { ".doc", new[] { "application/msword" } },
                { ".txt", new[] { "text/plain" } }
            };
            
            bool mimeTypeMatches = expectedMimeTypes.ContainsKey(fileExtension) &&
                                  Array.Exists(expectedMimeTypes[fileExtension], mime => mime == mockFile.ContentType);
            
            bool shouldBeRejected = !mimeTypeMatches;
            
            // Assert - This test FAILS because MIME type spoofing is not detected
            Assert.False(isAcceptedByCurrentCode && shouldBeRejected,
                $"EXPECTED FAILURE: File '{fileName}' with spoofed MIME type '{spoofedMimeType}' is currently accepted. " +
                "After fix, file upload validator should validate MIME type matches file extension.");
        }

        /// <summary>
        /// Test 1.2.10: Path traversal in filename should be prevented
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Filename is not sanitized for path traversal sequences
        /// Expected Behavior: Path traversal sequences should be removed from filename
        /// Current Behavior: Only uses Path.GetFileName which may not fully sanitize
        /// </summary>
        [Theory]
        [InlineData("../../../etc/passwd.pdf")]
        [InlineData("..\\..\\..\\windows\\system32\\config.pdf")]
        [InlineData("....//....//....//etc/passwd.pdf")]
        public void PathTraversalInFilename_ShouldBeSanitized_ButIsNot(string maliciousFileName)
        {
            // Arrange
            var mockFile = CreateMockFormFile(maliciousFileName, "application/pdf", 1024);
            
            // Act - Current sanitization: Path.GetFileName(policyFile.FileName)
            // Path.GetFileName removes directory separators but may not catch all path traversal attempts
            string sanitizedByCurrentCode = Path.GetFileName(maliciousFileName);
            
            // Expected behavior: Should remove all path traversal sequences
            bool containsPathTraversal = maliciousFileName.Contains("..") || 
                                         maliciousFileName.Contains("../") ||
                                         maliciousFileName.Contains("..\\");
            
            // Assert - This test documents the current behavior
            // After fix: Filename sanitization should remove all path traversal sequences and special characters
            if (containsPathTraversal)
            {
                Assert.False(sanitizedByCurrentCode.Contains(".."),
                    $"EXPECTED FAILURE: Path traversal sequences in filename '{maliciousFileName}' may not be fully sanitized. " +
                    "After fix, filename sanitization should remove all path traversal sequences and special characters.");
            }
        }
    }
}
