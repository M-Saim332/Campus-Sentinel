using System;
using System.Collections.Generic;
using System.IO;
using CampusSentinel.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;

namespace CampusSentinel.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateVisitorIdCard(Visitor visitor)
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = XUnit.FromInch(3.5);
            page.Height = XUnit.FromInch(2.2);
            var gfx = XGraphics.FromPdfPage(page);

            // Professional background
            var rect = new XRect(0, 0, page.Width, page.Height);
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(15, 23, 42)), rect);
            
            // Header
            var headerRect = new XRect(0, 0, page.Width, 40);
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(30, 41, 59)), headerRect);
            gfx.DrawString("CAMPUS VISITOR PASS", new XFont("Segoe UI", 12, XFontStyle.Bold), XBrushes.White, 
                new XRect(0, 0, page.Width, 40), XStringFormats.Center);

            // Visitor Details
            var fontMain = new XFont("Segoe UI", 10, XFontStyle.Regular);
            var fontBold = new XFont("Segoe UI", 10, XFontStyle.Bold);
            
            gfx.DrawString(visitor.FullName, new XFont("Segoe UI", 14, XFontStyle.Bold), XBrushes.White, 20, 65);
            gfx.DrawString($"Role: {visitor.Role}", fontMain, XBrushes.LightBlue, 20, 85);
            gfx.DrawString($"Expires: {visitor.ExpirationTime:g}", new XFont("Segoe UI", 8), XBrushes.Red, 20, 105);

            // QR Code
            byte[] qrBytes = GenerateQrCode(visitor.TemporaryQrCodeId);
            using var ms = new MemoryStream(qrBytes);
            var xImg = XImage.FromStream(() => ms);
            gfx.DrawImage(xImg, page.Width - 90, 55, 75, 75);

            using var outputMs = new MemoryStream();
            document.Save(outputMs);
            return outputMs.ToArray();
        }

        public byte[] GenerateStudentIdCard(Student student)
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = XUnit.FromInch(3.5);
            page.Height = XUnit.FromInch(2.2);
            var gfx = XGraphics.FromPdfPage(page);

            // Modern Design
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(15, 23, 42)), 0, 0, page.Width, page.Height);
            
            // Status bar for residency
            var statusColor = student.ResidencyType == "Hostelite" ? XColor.FromArgb(16, 185, 129) : XColor.FromArgb(59, 130, 246);
            gfx.DrawRectangle(new XSolidBrush(statusColor), 0, 0, 5, page.Height);

            gfx.DrawString("UNIVERSITY STUDENT ID", new XFont("Segoe UI", 10, XFontStyle.Bold), XBrushes.SkyBlue, 20, 30);
            gfx.DrawString(student.FullName, new XFont("Segoe UI", 13, XFontStyle.Bold), XBrushes.White, 20, 60);
            gfx.DrawString($"{student.Department} | {student.RegistrationNo}", new XFont("Segoe UI", 9), XBrushes.LightGray, 20, 80);
            gfx.DrawString($"{student.Gender.ToUpper()} | {student.ResidencyType.ToUpper()}", new XFont("Segoe UI", 9, XFontStyle.Bold), new XSolidBrush(statusColor), 20, 100);

            // QR Code
            byte[] qrBytes = GenerateQrCode(student.QrCodeId);
            using var ms = new MemoryStream(qrBytes);
            var xImg = XImage.FromStream(() => ms);
            gfx.DrawImage(xImg, page.Width - 90, 50, 80, 80);

            using var outputMs = new MemoryStream();
            document.Save(outputMs);
            return outputMs.ToArray();
        }

        public byte[] GenerateStaffIdCard(Staff staff)
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Width = XUnit.FromInch(3.5);
            page.Height = XUnit.FromInch(2.2);
            var gfx = XGraphics.FromPdfPage(page);

            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(15, 23, 42)), 0, 0, page.Width, page.Height);
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(99, 102, 241)), 0, 0, page.Width, 35);
            gfx.DrawString("FACULTY & STAFF", new XFont("Segoe UI", 11, XFontStyle.Bold), XBrushes.White, new XRect(0, 0, page.Width, 35), XStringFormats.Center);

            gfx.DrawString(staff.FullName, new XFont("Segoe UI", 13, XFontStyle.Bold), XBrushes.White, 20, 65);
            gfx.DrawString(staff.Designation, new XFont("Segoe UI", 10), XBrushes.LightGray, 20, 85);
            gfx.DrawString(staff.DepartmentOrUni, new XFont("Segoe UI", 10, XFontStyle.Bold), XBrushes.MediumPurple, 20, 105);

            byte[] qrBytes = GenerateQrCode(staff.StaffId);
            using var ms = new MemoryStream(qrBytes);
            var xImg = XImage.FromStream(() => ms);
            gfx.DrawImage(xImg, page.Width - 95, 55, 80, 80);

            using var outputMs = new MemoryStream();
            document.Save(outputMs);
            return outputMs.ToArray();
        }

        public byte[] GenerateWeeklyReport(List<AccessLog> logs)
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var fontTitle = new XFont("Segoe UI", 18, XFontStyle.Bold);
            var fontHeader = new XFont("Segoe UI", 10, XFontStyle.Bold);
            var fontBody = new XFont("Segoe UI", 10, XFontStyle.Regular);

            gfx.DrawString("Weekly Campus Access Performance Report", fontTitle, XBrushes.Black, new XRect(0, 40, page.Width, 40), XStringFormats.Center);
            gfx.DrawString($"Generated: {DateTime.Now:f}", fontBody, XBrushes.Gray, new XRect(0, 70, page.Width, 20), XStringFormats.Center);

            // Table Headers
            double y = 110;
            gfx.DrawRectangle(XBrushes.LightGray, 40, y, page.Width - 80, 20);
            gfx.DrawString("Time", fontHeader, XBrushes.Black, 50, y + 14);
            gfx.DrawString("ID", fontHeader, XBrushes.Black, 150, y + 14);
            gfx.DrawString("Type", fontHeader, XBrushes.Black, 250, y + 14);
            gfx.DrawString("Mode", fontHeader, XBrushes.Black, 350, y + 14);
            gfx.DrawString("Status", fontHeader, XBrushes.Black, 450, y + 14);

            y += 25;
            foreach (var log in logs)
            {
                if (y > page.Height - 50) { page = document.AddPage(); gfx = XGraphics.FromPdfPage(page); y = 50; }

                gfx.DrawString(log.Timestamp.ToString("MM/dd HH:mm"), fontBody, XBrushes.Black, 50, y);
                gfx.DrawString(log.TargetId, fontBody, XBrushes.Black, 150, y);
                gfx.DrawString(log.TargetType, fontBody, XBrushes.Black, 250, y);
                gfx.DrawString(log.Direction, fontBody, log.Direction == "Entry" ? XBrushes.Green : XBrushes.Blue, 350, y);
                gfx.DrawString(log.Status, fontBody, log.Status == "Granted" ? XBrushes.Black : XBrushes.Red, 450, y);
                y += 18;
            }

            using var outputMs = new MemoryStream();
            document.Save(outputMs);
            return outputMs.ToArray();
        }

        private byte[] GenerateQrCode(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }
}
