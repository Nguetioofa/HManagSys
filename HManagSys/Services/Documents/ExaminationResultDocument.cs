using HManagSys.Models.ViewModels.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HManagSys.Services.Documents;

/// <summary>
/// Document QuestPDF pour un résultat d'examen médical
/// </summary>
public class ExaminationResultDocument : IDocument
{
    private readonly ExaminationResultPdfViewModel _model;

    public ExaminationResultDocument(ExaminationResultPdfViewModel model)
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

            // Numéro d'examen
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text("RÉSULTAT D'EXAMEN")
                    .FontSize(14).Bold();
                col.Item().AlignRight().Text(_model.ExaminationNumber)
                    .FontSize(12);
                col.Item().AlignRight().Text(_model.PerformedDate)
                    .FontSize(9);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Information patient
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
                    c.Item().Text("TYPE D'EXAMEN").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(_model.ExaminationType).Bold();
                });
            });

            // Dates et responsables
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().Column(c =>
                {
                    c.Item().Text("DEMANDÉ PAR").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(_model.RequestedBy);
                    c.Item().Text($"Le {_model.RequestDate}");
                });

                table.Cell().Column(c =>
                {
                    c.Item().Text("RÉALISÉ PAR").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(_model.PerformedBy);
                    c.Item().Text($"Le {_model.PerformedDate}");
                });
            });

            // Séparateur
            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Résultats de l'examen
            col.Item().PaddingTop(5).Column(c =>
            {
                c.Item().Text("RÉSULTATS").FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                c.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(10)
                    .Text(_model.ResultData);
            });

            // Notes additionnelles
            if (!string.IsNullOrEmpty(_model.ResultNotes))
            {
                col.Item().PaddingTop(10).Column(c =>
                {
                    c.Item().Text("NOTES ADDITIONNELLES").FontSize(11).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Background(Colors.Grey.Lighten5).Padding(8)
                        .Text(_model.ResultNotes);
                });
            }

            // Information sur l'attachement
            if (!string.IsNullOrEmpty(_model.AttachmentPath))
            {
                col.Item().PaddingTop(10).Background(Colors.Yellow.Lighten5).Border(1).BorderColor(Colors.Yellow.Medium).Padding(10)
                    .Text(text =>
                    {
                        text.Span("Document annexe disponible: ").Bold();
                        text.Span(_model.AttachmentPath);
                    });
            }

            // Signature
            col.Item().PaddingTop(30).AlignRight().Column(signatureCol =>
            {
                signatureCol.Item().Text("Signature et cachet").Bold();
                signatureCol.Item().PaddingTop(30).Text(_model.PerformedBy);
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