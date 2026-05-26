using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Repositories;
using QRCoder;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;

namespace CampusSentinel.Pages.Admin.Visitors
{
    public class CreateModel : PageModel
    {
        private readonly IRepository<Visitor> _repository;
        private readonly IPersonRepository _personRepository;
        private readonly ApplicationDbContext _context;

        public CreateModel(IRepository<Visitor> repository, IPersonRepository personRepository, ApplicationDbContext context)
        {
            _repository = repository;
            _personRepository = personRepository;
            _context = context;
        }

        [BindProperty]
        public Visitor Visitor { get; set; }

        public string GeneratedQrCodeBase64 { get; set; }
        public string GeneratedBarcodeId { get; set; }

        public IActionResult OnGet()
        {
            Visitor = new Visitor
            {
                ExpirationTime = DateTime.Now.AddDays(1)
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Visitor.TemporaryQrCodeId");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Generate a unique 8-character alphanumeric string for the QR code
            string randomChars = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            string barcodeId = $"VISIT-{randomChars}";

            // Ensure uniqueness
            var existingVisitor = await _personRepository.GetVisitorByTemporaryQrCodeAsync(barcodeId);
            while (existingVisitor != null)
            {
                randomChars = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                barcodeId = $"VISIT-{randomChars}";
                existingVisitor = await _personRepository.GetVisitorByTemporaryQrCodeAsync(barcodeId);
            }

            Visitor.TemporaryQrCodeId = barcodeId;
            Visitor.CreatedAt = DateTime.Now;
            await _repository.AddAsync(Visitor);
            await _repository.SaveChangesAsync();

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(barcodeId, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                GeneratedQrCodeBase64 = Convert.ToBase64String(qrBytes);
                GeneratedBarcodeId = barcodeId;
            }
            catch (Exception ex) 
            { 
                GeneratedBarcodeId = "ERROR: " + ex.Message;
            }

            ModelState.Clear();
            Visitor = new Visitor { ExpirationTime = DateTime.Now.AddDays(1) };
            return Page();
        }
    }
}
