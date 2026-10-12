using System.ComponentModel.DataAnnotations;

namespace BibliotecaApi.Dtos;

public record LivroDto(
    [Required, MaxLength(200)] string Titulo,
    [Required, MaxLength(150)] string Autor,
    [Range(0, 2100)] int? AnoPublicacao,
    [MaxLength(80)] string? Genero
);
