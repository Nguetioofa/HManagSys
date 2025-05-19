using HManagSys.Models.ViewModels.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HManagSys.Services.Documents;

/// <summary>
/// Document QuestPDF pour une prescription médicale
/// </summary>
public class PrescriptionDocument : IDocument
{
    private readonly PrescriptionPdfViewModel _model;

    public PrescriptionDocument(PrescriptionPdfViewModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
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

            // Numéro de prescription
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text("PRESCRIPTION MÉDICALE")
                    .FontSize(14).Bold();
                col.Item().AlignRight().Text(_model.PrescriptionNumber)
                    .FontSize(12);
                col.Item().AlignRight().Text(_model.PrescriptionDate)
                    .FontSize(9);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Information patient et médecin
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PATIENT").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(_model.PatientName).Bold();
                    c.Item().Text(_model.PatientInfo);
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("MÉDECIN").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(_model.DoctorName).Bold();
                });
            });

            // Diagnostic
            if (!string.IsNullOrEmpty(_model.DiagnosisName))
            {
                col.Item().PaddingTop(10).Column(c =>
                {
                    c.Item().Text("DIAGNOSTIC").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(_model.DiagnosisName);
                });
            }

            // Instructions générales
            if (!string.IsNullOrEmpty(_model.Instructions))
            {
                col.Item().PaddingTop(10).Column(c =>
                {
                    c.Item().Text("INSTRUCTIONS GÉNÉRALES").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Background(Colors.Grey.Lighten4).Padding(5)
                        .Text(_model.Instructions);
                });
            }

            // Liste des médicaments
            col.Item().PaddingTop(15).Column(c =>
            {
                c.Item().Text("MÉDICAMENTS PRESCRITS").FontSize(11).Bold().FontColor(Colors.Blue.Medium);

                if (_model.Items.Any())
                {
                    c.Item().Table(table =>
                    {
                        // Définition des colonnes
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        // En-têtes
                        table.Header(header =>
                        {
                            header.Cell().Text("Médicament").Bold();
                            header.Cell().Text("Qté").Bold();
                            header.Cell().Text("Dosage").Bold();
                            header.Cell().Text("Fréquence").Bold();
                            header.Cell().Text("Instructions").Bold();
                        });

                        // Données
                        foreach (var item in _model.Items)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text($"{item.Quantity} {item.UnitOfMeasure}");
                            table.Cell().Text(item.Dosage);
                            table.Cell().Text(item.Frequency);
                            table.Cell().Text(item.Instructions);
                        }
                    });
                }
                else
                {
                    c.Item().Text("Aucun médicament prescrit").Italic();
                }
            });

            // Texte légal
            col.Item().PaddingTop(20).Text(text =>
            {
                text.Span("Cette prescription est valable pour une durée d'un mois à compter de la date d'émission.")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });

            // Signature
            col.Item().PaddingTop(30).AlignRight().Column(signatureCol =>
            {
                signatureCol.Item().Text("Signature et cachet").Bold();
                signatureCol.Item().PaddingTop(30).Text(_model.DoctorName);
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