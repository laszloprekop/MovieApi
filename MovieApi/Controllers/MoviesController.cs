using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.DTOs;
using MovieApi.Models;

namespace MovieApi.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly MovieContext _context;
    public MoviesController(MovieContext context) => _context = context;

    // GET: api/movies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] string? genre,
        [FromQuery] int? year,
        [FromQuery] string? actor)
    {
        var query = _context.Movies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(genre)) query = query.Where(m => m.Genre == genre);
        if (year is not null) query = query.Where(m => m.Year == year);
        if (!string.IsNullOrWhiteSpace(actor)) query = query.Where(m => m.Actors.Any(a => a.Name == actor));

        var movies = await query
            .Select(m => new MovieDto
            {
                Id = m.Id, Title = m.Title, Year = m.Year, Genre = m.Genre, Duration = m.Duration
            }).ToListAsync();
        return Ok(movies);
    }

    // GET: api/movies/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDto>> GetMovie(int id)
    {
        var movie = await _context.Movies.Where(m => m.Id == id)
            .Select(m => new MovieDto()
            {
                Id = m.Id, Title = m.Title, Year = m.Year, Genre = m.Genre, Duration = m.Duration
            }).FirstOrDefaultAsync();

        if (movie is null) return NotFound();

        return Ok(movie);
    }

    // GET /api/movies/{id}/details
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<MovieDetailDto>> GetMovieDetails(int id)
    {
        var dto = await _context.Movies
            .Where(m => m.Id == id)
            .Select(m => new MovieDetailDto
            {
                Id = m.Id, Title = m.Title, Year = m.Year, Genre = m.Genre, Duration = m.Duration,
                Synopsis = m.Details!.Synopsis,
                Language = m.Details.Language,
                Budget = m.Details.Budget,
                Reviews = m.Reviews.Select(r => new ReviewDto
                {
                    Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
                }).ToList(),
                Actors = m.Actors.Select(a => new ActorDto
                {
                    Id = a.Id, Name = a.Name, BirthYear = a.BirthYear
                }).ToList()
            }).FirstOrDefaultAsync();
        if (dto is null) return NotFound();
        return Ok(dto);
    }
    
    // PUT: api/Movie/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMovie(int? id, Movie movie)
    {
        if (id != movie.Id)
        {
            return BadRequest();
        }

        _context.Entry(movie).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MovieExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Movie
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Movie>> PostMovie(Movie movie)
    {
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetMovie", new { id = movie.Id }, movie);
    }

    // DELETE: api/Movie/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMovie(int? id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    private bool MovieExists(int? id)
    {
        return _context.Movies.Any(e => e.Id == id);
    }
}