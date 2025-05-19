//using HManagSys.Models.ViewModels.Payments;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

//namespace HManagSys.Services.Documents
//{
//    public class PaymentReceiptDocument : IDocument
//    {
//        private readonly ReceiptViewModel _receiptModel;

//        public PaymentReceiptDocument(ReceiptViewModel receiptModel)
//        {
//            _receiptModel = receiptModel;
//        }

//        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

//        public void Compose(IDocumentContainer container)
//        {
//            container
//                .Page(page =>
//                {
//                    page.Margin(50);

//                    page.Header().Element(ComposeHeader);
//                    page.Content().Element(ComposeContent);
//                    page.Footer().Element(ComposeFooter);
//                });
//        }

//        private void ComposeHeader(IContainer container)
//        {
//            container.Row(row =>
//            {
//                row.RelativeItem().Column(column =>
//                {
//                    column.Item().Text(_receiptModel.HospitalName)
//                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

//                    column.Item().Text(_receiptModel.HospitalAddress)
//                        .FontSize(9);

//                    if (!string.IsNullOrEmpty(_receiptModel.HospitalContact))
//                    {
//                        column.Item().Text(_receiptModel.HospitalContact)
//                            .FontSize(9);
//                    }
//                });

//                row.RelativeItem().Column(column =>
//                {
//                    column.Item().AlignRight().Text("REÇU DE PAIEMENT")
//                        .Bold().FontSize(14);

//                    column.Item().AlignRight().Text($"N° {_receiptModel.ReceiptNumber}")
//                        .FontSize(10);

//                    column.Item().AlignRight().Text($"Date: {_receiptModel.ReceiptDate:dd/MM/yyyy HH:mm}")
//                        .FontSize(10);
//                });
//            });
//        }

//        private void ComposeContent(IContainer container)
//        {
//            container.PaddingVertical(20).Column(column =>
//            {
//                // Informations du patient et référence
//                column.Item().PaddingTop(10).Element(ComposeClientSection);

//                // Détails du paiement
//                column.Item().PaddingTop(10).Element(ComposePaymentDetails);

//                // Montant
//                column.Item().PaddingTop(10).Element(ComposeAmount);

//                // Notes (si présentes)
//                if (!string.IsNullOrWhiteSpace(_receiptModel.Payment.Notes))
//                {
//                    column.Item().PaddingTop(10).Element(ComposeNotes);
//                }
//            });
//        }

//        private void ComposeClientSection(IContainer container)
//        {
//            container.BorderTop(1).BorderBottom(1).PaddingVertical(10)
//                .Column(column =>
//                {
//                    if (_receiptModel.Payment.PatientId.HasValue)
//                    {
//                        column.Item().Text(text =>
//                        {
//                            text.Span("Patient: ").SemiBold();
//                            text.Span(_receiptModel.Payment.PatientName);
//                        });
//                    }

//                    column.Item().Text(text =>
//                    {
//                        text.Span("Référence: ").SemiBold();
//                        text.Span(_receiptModel.Payment.ReferenceDescription);
//                    });

//                    column.Item().Text(text =>
//                    {
//                        text.Span("Type: ").SemiBold();
//                        text.Span(_receiptModel.Payment.ReferenceType);
//                    });
//                });
//        }

//        private void ComposePaymentDetails(IContainer container)
//        {
//            container.PaddingVertical(10)
//                .Column(column =>
//                {
//                    column.Item().Text("Détails du paiement").SemiBold().FontSize(12);

//                    column.Item().PaddingTop(5).Table(table =>
//                    {
//                        table.ColumnsDefinition(columns =>
//                        {
//                            columns.RelativeColumn(3);
//                            columns.RelativeColumn(7);
//                        });

//                        table.Cell().Text("Méthode de paiement:").SemiBold();
//                        table.Cell().Text(_receiptModel.Payment.PaymentMethodName);

//                        table.Cell().Text("Date de paiement:").SemiBold();
//                        table.Cell().Text(_receiptModel.Payment.PaymentDate.ToString("dd/MM/yyyy HH:mm"));

//                        table.Cell().Text("Reçu par:").SemiBold();
//                        table.Cell().Text(_receiptModel.Payment.ReceivedByName);

//                        if (!string.IsNullOrWhiteSpace(_receiptModel.Payment.TransactionReference))
//                        {
//                            table.Cell().Text("Référence transaction:").SemiBold();
//                            table.Cell().Text(_receiptModel.Payment.TransactionReference);
//                        }
//                    });
//                });
//        }

//        private void ComposeAmount(IContainer container)
//        {
//            container.Background(Colors.Grey.Lighten3)
//                .Padding(10)
//                .Column(column =>
//                {
//                    column.Item().AlignCenter().Text("MONTANT PAYÉ").SemiBold();

//                    column.Item().AlignCenter().Text(_receiptModel.Payment.FormattedAmount)
//                        .Bold().FontSize(16).FontColor(Colors.Green.Medium);

//                    if (_receiptModel.Payment.IsCancelled)
//                    {
//                        column.Item().AlignCenter().Text("*** ANNULÉ ***")
//                            .Bold().FontSize(14).FontColor(Colors.Red.Medium);
//                    }
//                });
//        }

//        private void ComposeNotes(IContainer container)
//        {
//            container.Column(column =>
//            {
//                column.Item().Text("Notes:").SemiBold();
//                column.Item().PaddingLeft(5).Text(_receiptModel.Payment.Notes)
//                    .FontSize(9);
//            });
//        }

//        private void ComposeFooter(IContainer container)
//        {
//            container.Column(column =>
//            {
//                column.Item().AlignCenter().Text("Merci pour votre paiement")
//                    .FontSize(10);

//                column.Item().AlignCenter().Text("Ce document tient lieu de reçu officiel")
//                    .FontSize(8).FontColor(Colors.Grey.Medium);

//                column.Item().AlignCenter().Text($"Émis le {DateTime.Now:dd/MM/yyyy HH:mm}")
//                    .FontSize(8).FontColor(Colors.Grey.Medium);
//            });
//        }
//    }
//}

using HManagSys.Models.ViewModels.Documents;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HManagSys.Services.Documents;

/// <summary>
/// Document QuestPDF pour un reçu de paiement
/// </summary>
public class PaymentReceiptDocument : IDocument
{
    private readonly ReceiptPdfViewModel _model;

    public PaymentReceiptDocument(ReceiptPdfViewModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Logo ou titre du centre
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_model.HospitalName)
                    .FontSize(16).Bold();
                col.Item().Text(_model.HospitalAddress)
                    .FontSize(9);
                col.Item().Text(_model.HospitalContact)
                    .FontSize(9);
            });

            // Numéro de reçu
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text("REÇU DE PAIEMENT")
                    .FontSize(14).Bold();
                col.Item().AlignRight().Text(_model.ReceiptNumber)
                    .FontSize(12);
                col.Item().AlignRight().Text(_model.PaymentDate)
                    .FontSize(9);

                if (_model.IsCancelled)
                {
                    col.Item().AlignRight().Text("ANNULÉ")
                        .FontSize(16).Bold().FontColor(Colors.Red.Medium);
                }
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Information patient et référence
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("INFORMATIONS CLIENT").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text($"Patient: {_model.PatientName}");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("DÉTAILS DE RÉFÉRENCE").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text($"Type: {_model.ReferenceType}");
                    c.Item().Text($"{_model.ReferenceDetails}");
                });
            });

            // Séparateur
            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Détails du paiement
            col.Item().PaddingVertical(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("DÉTAILS DU PAIEMENT").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text($"Méthode: {_model.PaymentMethod}");

                    if (!string.IsNullOrEmpty(_model.TransactionReference))
                    {
                        c.Item().Text($"Référence: {_model.TransactionReference}");
                    }

                    c.Item().Text($"Reçu par: {_model.ReceivedBy}");
                });

                // Montant en grand
                row.RelativeItem().AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text("MONTANT PAYÉ").FontSize(10);
                    c.Item().Text(_model.FormattedAmount)
                        .FontSize(18).Bold();
                });
            });

            // Notes
            if (!string.IsNullOrEmpty(_model.Notes))
            {
                col.Item().PaddingTop(10).Column(c =>
                {
                    c.Item().Text("NOTES").FontSize(10).Bold();
                    c.Item().Background(Colors.Grey.Lighten4).Padding(5)
                        .Text(_model.Notes).FontSize(9);
                });
            }

            // Raison d'annulation
            if (_model.IsCancelled && !string.IsNullOrEmpty(_model.CancellationReason))
            {
                col.Item().PaddingTop(10).Column(c =>
                {
                    c.Item().Text("ANNULATION").FontSize(10).Bold().FontColor(Colors.Red.Medium);
                    c.Item().Background(Colors.Red.Lighten4).Padding(5)
                        .Text(_model.CancellationReason).FontSize(9);
                });
            }

            // Texte légal
            col.Item().PaddingTop(20).AlignCenter().Text(text =>
            {
                text.Span("Ce document tient lieu de reçu officiel").FontSize(8);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_model.FooterText).FontSize(8).FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8);
                text.CurrentPageNumber().FontSize(8);
                text.Span(" / ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }

    public byte[] GeneratePdf()
    {
        return Document.Create(container => Compose(container)).GeneratePdf();
    }
}