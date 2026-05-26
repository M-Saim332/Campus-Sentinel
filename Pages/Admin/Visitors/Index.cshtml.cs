using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Repositories;
using QRCoder;

namespace CampusSentinel.Pages.Admin.Visitors
{
    public class IndexModel : PageModel
    {
        private readonly IRepository<Visitor> _repository;

        public IndexModel(IRepository<Visitor> repository)
        {
            _repository = repository;
        }

        public IEnumerable<Visitor> Visitors { get; set; }

        public async Task OnGetAsync()
        {
            Visitors = await _repository.GetAllAsync();
        }

        /// <summary>
        /// GET /Admin/Visitors?handler=QrImage&qrId=TEMP-123
        /// Returns a PNG QR code for the given qrId.
        /// </summary>
        public IActionResult OnGetQrImage(string qrId)
        {
            if (string.IsNullOrWhiteSpace(qrId))
                return BadRequest();

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrId, QRCodeGenerator.ECCLevel.Q);
                var qrCode     = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20); // Higher resolution
                
                string fileName = $"QR_{qrId}.png";
                return File(qrBytes, "image/png", fileName);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
