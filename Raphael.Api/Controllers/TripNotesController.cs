using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Attributes;
using Raphael.Api.Services.Auth;
using Raphael.Api.Services.Places;
using Raphael.Shared.DTOs.Places;
using Raphael.Shared.Entities.Places;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// Notes of a trip's places and patient, the guide the driver reads at each stop (RE-054).
    /// </summary>
    /// <remarks>
    /// Everything hangs off a trip: whoever may see the trip may read and edit its notes, and a
    /// driver only the trips of their own routes (404 otherwise). The office (Desktop) and the
    /// driver (Driver) only — never a clinic, an external Provider or the Rider app: the notes
    /// describe homes and patients (PHI). Note text is never logged.
    /// </remarks>
    [ApiController]
    [Authorize]
    [NotForClinicUsers]
    [AllowApps(ClientApps.Desktop, ClientApps.Driver)]
    [Route("api/trips/{tripId:int}/notes")]
    public class TripNotesController : ControllerBase
    {
        private readonly ITripNotesService _notes;

        public TripNotesController(ITripNotesService notes)
        {
            _notes = notes;
        }

        /// <summary>The notes window of a trip: both ends, their places, the patient.</summary>
        [HttpGet]
        public Task<ActionResult<TripNotesDto>> Get(int tripId) => Run(() => _notes.GetAsync(tripId));

        /// <summary>The note of this trip only, at one end. Empty text clears it. 409 on a past or closed trip.</summary>
        [HttpPut("{side}/trip-note")]
        public Task<ActionResult<TripNotesDto>> SetTripNote(int tripId, TripSide side, [FromBody] TripNoteUpdateDto dto) =>
            Run(() => _notes.SetTripNoteAsync(tripId, side, dto.Text));

        /// <summary>Turns this trip's own note (or one line of it) into a note of the place or of the patient.</summary>
        [HttpPost("{side}/trip-note/promote")]
        public Task<ActionResult<TripNotesDto>> Promote(int tripId, TripSide side, [FromBody] PromoteTripNoteDto dto) =>
            Run(() => _notes.PromoteTripNoteAsync(tripId, side, dto));

        /// <summary>A new note on the place of one end. Reaches every trip still to come there.</summary>
        [HttpPost("{side}/place-notes")]
        public Task<ActionResult<TripNotesDto>> AddPlaceNote(int tripId, TripSide side, [FromBody] NoteUpsertDto dto) =>
            Run(() => _notes.AddPlaceNoteAsync(tripId, side, dto));

        [HttpPut("place-notes/{noteId:int}")]
        public Task<ActionResult<TripNotesDto>> UpdatePlaceNote(int tripId, int noteId, [FromBody] NoteUpsertDto dto) =>
            Run(() => _notes.UpdatePlaceNoteAsync(tripId, noteId, dto));

        [HttpDelete("place-notes/{noteId:int}")]
        public Task<ActionResult<TripNotesDto>> RemovePlaceNote(int tripId, int noteId) =>
            Run(() => _notes.RemovePlaceNoteAsync(tripId, noteId));

        /// <summary>A new note on the patient. Reaches every trip of theirs still to come.</summary>
        [HttpPost("customer-notes")]
        public Task<ActionResult<TripNotesDto>> AddCustomerNote(int tripId, [FromBody] NoteUpsertDto dto) =>
            Run(() => _notes.AddCustomerNoteAsync(tripId, dto));

        [HttpPut("customer-notes/{noteId:int}")]
        public Task<ActionResult<TripNotesDto>> UpdateCustomerNote(int tripId, int noteId, [FromBody] NoteUpsertDto dto) =>
            Run(() => _notes.UpdateCustomerNoteAsync(tripId, noteId, dto));

        [HttpDelete("customer-notes/{noteId:int}")]
        public Task<ActionResult<TripNotesDto>> RemoveCustomerNote(int tripId, int noteId) =>
            Run(() => _notes.RemoveCustomerNoteAsync(tripId, noteId));

        /// <summary>Points one end at another existing place (the address matched the wrong one, or none).</summary>
        [HttpPut("{side}/place")]
        public Task<ActionResult<TripNotesDto>> LinkPlace(int tripId, TripSide side, [FromBody] PlaceLinkDto dto) =>
            Run(() => _notes.LinkPlaceAsync(tripId, side, dto.PlaceId));

        /// <summary>Renames the place of one end, changes its kind, or names the entrance it stops at.</summary>
        [HttpPatch("{side}/place")]
        public Task<ActionResult<TripNotesDto>> UpdatePlace(int tripId, TripSide side, [FromBody] PlaceUpdateDto dto) =>
            Run(() => _notes.UpdatePlaceAsync(tripId, side, dto));

        private async Task<ActionResult<TripNotesDto>> Run(Func<Task<TripNotesDto?>> action)
        {
            try
            {
                var result = await action();
                return result == null ? NotFound() : Ok(result);
            }
            catch (NotesValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    /// <summary>Facility search and the notes preview of the trip form (RE-054). Office only.</summary>
    [ApiController]
    [Authorize]
    [NotForClinicUsers]
    [Route("api/places")]
    public class PlacesController : ControllerBase
    {
        private readonly ITripNotesService _notes;

        public PlacesController(ITripNotesService notes)
        {
            _notes = notes;
        }

        /// <summary>Facilities by name or address, to link a trip end to the right one. Homes are not listed.</summary>
        [HttpGet]
        public async Task<ActionResult<List<PlaceSummaryDto>>> Search([FromQuery] string? search, [FromQuery] int take = 20) =>
            Ok(await _notes.SearchPlacesAsync(search, take));

        /// <summary>
        /// What a trip about to be created would carry for the driver. POST because the body holds a
        /// patient's addresses, which never go in a query string. Writes nothing.
        /// </summary>
        [HttpPost("notes-preview")]
        public async Task<ActionResult<NotesPreviewDto>> Preview([FromBody] NotesPreviewRequestDto request) =>
            Ok(await _notes.PreviewAsync(request));
    }
}
