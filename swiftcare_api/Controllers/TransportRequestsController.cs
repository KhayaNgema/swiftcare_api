using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcare_api.Data;
using swiftcare_api.DTOs;
using swiftcare_api.Models;
using System.Text;
using System.Text.Json;

namespace swiftcare_api.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransportRequestsApiController : ControllerBase
    {
        private readonly swiftcare_apiDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<UserBaseModel> _userManager;


        public TransportRequestsApiController(
            swiftcare_apiDbContext context,
            UserManager<UserBaseModel> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // ============================================================
        // GET DRIVER'S TRANSPORT REQUESTS
        //
        // RETURNS ONLY:
        // REQUESTS ASSIGNED TO THE LOGGED-IN DRIVER
        // ============================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("my_requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            // ========================================================
            // GET LOGGED-IN USER FROM JWT
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }

            // ========================================================
            // VERIFY LOGGED-IN USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }

            // ========================================================
            // GET ONLY TRANSPORT REQUESTS ASSIGNED TO THIS DRIVER
            // ========================================================

            var requests = await _context.TransportRequests
                .AsNoTracking()
                .Where(r => r.AssignedToId == driver.Id)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new TransportRequestDto
                {
                    RequestId = r.RequestId,

                    RequestedForId = r.RequestedForId,

                    RequestReason = r.RequestReason,

                    RequestedForDate = r.RequestedForDate,

                    RequestedForTime = r.RequestedForTime,

                    PickUpAddress = r.PickUpAddress,

                    DestinationAddress = r.DestinationAddress,

                    PickUpLatitude = r.PickUpLatitude,

                    PickUpLongitude = r.PickUpLongitude,

                    DestinationLatitude = r.DestinationLatitude,

                    DestinationLongitude = r.DestinationLongitude,

                    RequestedAt = r.RequestedAt,

                    TravelDistance = r.TravelDistance,

                    TravelTime = r.TravelTime,

                    RequestStatus = r.Status,

                    AssignedToId = r.AssignedToId,

                    VehicleId = r.VehicleId,

                    // ========================================
                    // RESIDENT
                    // ========================================

                    ResidentFirstName = r.Resident.FirstName,

                    ResidentLastName = r.Resident.LastName,

                    ResidentProfilePicture = r.Resident.ProfilePicture,

                    RoomNumber = r.Resident.RoomNumber
                })
                .ToListAsync();

            return Ok(requests);
        }

        // ============================================================
        // GET GOOGLE DRIVING ROUTE - ROUTES API
        // ============================================================

        [HttpGet("get_route")]
        public async Task<IActionResult> GetRoute(
            double originLat,
            double originLng,
            double destinationLat,
            double destinationLng)
        {
            try
            {
                var googleApiKey =
                    _configuration["GoogleMaps:ApiKey"];

                if (string.IsNullOrWhiteSpace(googleApiKey))
                {
                    return BadRequest(new
                    {
                        message = "Google Maps API key is not configured."
                    });
                }

                // ========================================================
                // ROUTES API ENDPOINT
                // ========================================================

                var googleUrl =
                    "https://routes.googleapis.com/directions/v2:computeRoutes";

                // ========================================================
                // REQUEST BODY
                // ========================================================

                var requestBody = new
                {
                    origin = new
                    {
                        location = new
                        {
                            latLng = new
                            {
                                latitude = originLat,
                                longitude = originLng
                            }
                        }
                    },

                    destination = new
                    {
                        location = new
                        {
                            latLng = new
                            {
                                latitude = destinationLat,
                                longitude = destinationLng
                            }
                        }
                    },

                    travelMode = "DRIVE",

                    routingPreference = "TRAFFIC_AWARE",

                    computeAlternativeRoutes = false,

                    languageCode = "en-US",

                    units = "METRIC"
                };

                // ========================================================
                // HTTP CLIENT
                // ========================================================

                using var client = new HttpClient();

                client.DefaultRequestHeaders.Add(
                    "X-Goog-Api-Key",
                    googleApiKey
                );

                client.DefaultRequestHeaders.Add(
                    "X-Goog-FieldMask",
                    "routes.duration,routes.distanceMeters,routes.polyline.encodedPolyline"
                );

                // ========================================================
                // SERIALIZE REQUEST
                // ========================================================

                var json = JsonSerializer.Serialize(requestBody);

                using var requestContent =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                // ========================================================
                // CALL ROUTES API
                // ========================================================

                var response =
                    await client.PostAsync(
                        googleUrl,
                        requestContent
                    );

                var content =
                    await response.Content.ReadAsStringAsync();

                // ========================================================
                // RETURN GOOGLE RESPONSE
                // ========================================================

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        new
                        {
                            message = "Google Routes API returned an error.",
                            googleResponse = content
                        }
                    );
                }

                return Content(
                    content,
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Unable to get the driving route.",
                        error = ex.Message
                    }
                );
            }
        }

        // ============================================================
        // GET MY VEHICLE
        //
        // RETURNS THE VEHICLE ASSOCIATED WITH THE LOGGED-IN DRIVER'S
        // MOST RECENT TRANSPORT REQUEST
        // ============================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("my_vehicle")]
        public async Task<ActionResult<MyVehicleDto>> GetMyVehicle()
        {
            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // GET THE MOST RECENT TRANSPORT REQUEST
            // ASSIGNED TO THIS DRIVER THAT HAS A VEHICLE
            // ========================================================

            var vehicle = await _context.TransportRequests
                .AsNoTracking()

                .Where(r =>
                    r.AssignedToId == driver.Id &&
                    r.VehicleId != null)

                .OrderByDescending(r => r.RequestedAt)

                .Select(r => new MyVehicleDto
                {
                    VehicleId = r.Vehicle.VehicleId,

                    VehicleName = r.Vehicle.VehicleName,

                    RegNumber = r.Vehicle.RegNumber,

                    Model = r.Vehicle.Model,

                    Year = r.Vehicle.Year,

                    Status = r.Vehicle.Status
                })

                .FirstOrDefaultAsync();


            // ========================================================
            // NO VEHICLE FOUND
            // ========================================================

            if (vehicle == null)
            {
                return NotFound(new
                {
                    message =
                        "No vehicle is currently associated with the logged-in driver."
                });
            }


            // ========================================================
            // RETURN VEHICLE DTO
            // ========================================================

            return Ok(vehicle);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("trip_details")]
        public async Task<ActionResult<TripDto>> GetTripDetails(int requestId)
        {
            // ============================================================
            // GET CURRENT USER
            // ============================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }

            // ============================================================
            // FIND DRIVER
            // ============================================================

            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }

            // ============================================================
            // FIND TRANSPORT REQUEST
            // ============================================================

            var trip = await _context.TransportRequests
                .AsNoTracking()
                .Where(r =>
                    r.RequestId == requestId &&
                    r.AssignedToId == driver.Id)
                .Select(r => new TripDto
                {
                    // ====================================================
                    // TRIP INFORMATION
                    // ====================================================

                    TripId = r.Trip != null
                        ? r.Trip.TripId
                        : 0,

                    RequestId = r.RequestId,

                    // ====================================================
                    // IMPORTANT:
                    // THIS IS THE TRANSPORT REQUEST STATUS
                    //
                    // Examples:
                    // Pending
                    // Driver_Approved
                    // Driver_Rejected
                    // ====================================================

                    RequestStatus = r.Status,

                    // ====================================================
                    // THIS IS THE TRIP STATUS
                    //
                    // Examples:
                    // Not_Started
                    // Started
                    // Ended
                    // ====================================================

                    Status = r.Trip != null
                        ? r.Trip.Status
                        : TripStatus.Not_Started,

                    // ====================================================
                    // TRIP DATES
                    // ====================================================

                    StartedAt = r.Trip != null
                        ? r.Trip.StartedAt
                        : null,

                    EndedAt = r.Trip != null
                        ? r.Trip.EndedAt
                        : null,

                    // ====================================================
                    // RESIDENT
                    // ====================================================

                    ResidentId = r.RequestedForId,

                    ResidentFirstName =
                        r.Resident.FirstName,

                    ResidentLastName =
                        r.Resident.LastName,

                    ResidentProfilePicture =
                        r.Resident.ProfilePicture,

                    RoomNumber =
                        r.Resident.RoomNumber,

                    // ====================================================
                    // PICKUP
                    // ====================================================

                    PickupAddress =
                        r.PickUpAddress,

                    PickupLatitude =
                        r.PickUpLatitude,

                    PickupLongitude =
                        r.PickUpLongitude,

                    // ====================================================
                    // DESTINATION
                    // ====================================================

                    DestinationAddress =
                        r.DestinationAddress,

                    DestinationLatitude =
                        r.DestinationLatitude,

                    DestinationLongitude =
                        r.DestinationLongitude,

                    // ====================================================
                    // REQUEST DETAILS
                    // ====================================================

                    RequestReason =
                        r.RequestReason,

                    RequestedForDate =
                        r.RequestedForDate,

                    RequestedForTime =
                        r.RequestedForTime,

                    RequestedAt =
                        r.RequestedAt,

                    // ====================================================
                    // TRAVEL INFORMATION
                    // ====================================================

                    TravelDistance =
                        r.TravelDistance,

                    TravelTime =
                        r.TravelTime,

                    CurrentLatitude = r.Trip != null
    ? r.Trip.CurrentLatitude
    : null,

                    CurrentLongitude = r.Trip != null
    ? r.Trip.CurrentLongitude
    : null,
                })
                .FirstOrDefaultAsync();

            // ============================================================
            // NOT FOUND
            // ============================================================

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Trip not found or you are not authorized to access it."
                });
            }

            // ============================================================
            // RETURN
            // ============================================================

            return Ok(trip);
        }
        // ============================================================
        // GET MY ACTIVE TRIPS
        //
        // RETURNS ACTIVE TRANSPORT REQUESTS ASSIGNED TO THE
        // CURRENTLY LOGGED-IN DRIVER
        // ============================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("my_trips")]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetMyTrips()
        {
            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // GET ACTIVE TRIPS
            //
            // IMPORTANT:
            // ONLY RETURN TRANSPORT REQUESTS:
            //
            // 1. ASSIGNED TO THIS DRIVER
            // 2. APPROVED BY THE DRIVER
            // 3. THAT ACTUALLY HAVE A TRIP
            // ========================================================

            var trips = await _context.TransportRequests
                .AsNoTracking()

                .Where(r =>
                    r.AssignedToId == driver.Id &&
                    r.Status == RequestStatus.Driver_Approved &&
                    r.Trip != null)

                .OrderByDescending(r => r.RequestedAt)

                .Select(r => new TripDto
                {
                    // ====================================================
                    // TRIP INFORMATION
                    // ====================================================

                    TripId = r.Trip!.TripId,

                    RequestId = r.RequestId,

                    Status = r.Trip.Status,

                    StartedAt = r.Trip.StartedAt,

                    EndedAt = r.Trip.EndedAt,


                    // ====================================================
                    // RESIDENT
                    // ====================================================

                    ResidentId = r.RequestedForId,

                    ResidentFirstName = r.Resident.FirstName,

                    ResidentLastName = r.Resident.LastName,

                    ResidentProfilePicture =
                        r.Resident.ProfilePicture,

                    RoomNumber =
                        r.Resident.RoomNumber,


                    // ====================================================
                    // PICKUP LOCATION
                    // ====================================================

                    PickupAddress =
                        r.PickUpAddress,

                    PickupLatitude =
                        r.PickUpLatitude,

                    PickupLongitude =
                        r.PickUpLongitude,


                    // ====================================================
                    // DESTINATION LOCATION
                    // ====================================================

                    DestinationAddress =
                        r.DestinationAddress,

                    DestinationLatitude =
                        r.DestinationLatitude,

                    DestinationLongitude =
                        r.DestinationLongitude,


                    // ====================================================
                    // REQUEST INFORMATION
                    // ====================================================

                    RequestReason =
                        r.RequestReason,

                    RequestedForDate =
                        r.RequestedForDate,

                    RequestedForTime =
                        r.RequestedForTime,

                    RequestedAt =
                        r.RequestedAt,


                    // ====================================================
                    // ROUTE INFORMATION
                    // ====================================================

                    TravelDistance =
                        r.TravelDistance,

                    TravelTime =
                        r.TravelTime
                })

                .ToListAsync();


            // ========================================================
            // RETURN ACTIVE TRIPS
            // ========================================================

            return Ok(trips);
        }

        // ============================================================
        // GET TRIP HISTORY
        //
        // RETURNS ONLY COMPLETED / ENDED TRIPS
        // BELONGING TO THE CURRENTLY LOGGED-IN DRIVER
        // ============================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("trip_history")]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetTripHistory()
        {
            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // GET COMPLETED / ENDED TRIPS
            //
            // QUERY THE TRIPS TABLE DIRECTLY
            //
            // Trip
            //   └── TransportRequest
            //         ├── Resident
            //         └── Assigned Driver
            // ========================================================

            var trips = await _context.Trips
                .AsNoTracking()

                .Where(t =>
                    t.TransportRequest != null &&
                    t.TransportRequest.AssignedToId == driver.Id &&
                    t.Status == TripStatus.Ended)

                .OrderByDescending(t => t.EndedAt)

                .Select(t => new TripDto
                {
                    // ====================================================
                    // TRIP INFORMATION
                    // ====================================================

                    TripId =
                        t.TripId,

                    RequestId =
                        t.RequestId,

                    Status =
                        t.Status,

                    StartedAt =
                        t.StartedAt,

                    EndedAt =
                        t.EndedAt,


                    // ====================================================
                    // RESIDENT
                    // ====================================================

                    ResidentId =
                        t.TransportRequest.RequestedForId,

                    ResidentFirstName =
                        t.TransportRequest.Resident.FirstName,

                    ResidentLastName =
                        t.TransportRequest.Resident.LastName,

                    ResidentProfilePicture =
                        t.TransportRequest.Resident.ProfilePicture,

                    RoomNumber =
                        t.TransportRequest.Resident.RoomNumber,


                    // ====================================================
                    // PICKUP LOCATION
                    // ====================================================

                    PickupAddress =
                        t.TransportRequest.PickUpAddress,

                    PickupLatitude =
                        t.TransportRequest.PickUpLatitude,

                    PickupLongitude =
                        t.TransportRequest.PickUpLongitude,


                    // ====================================================
                    // DESTINATION LOCATION
                    // ====================================================

                    DestinationAddress =
                        t.TransportRequest.DestinationAddress,

                    DestinationLatitude =
                        t.TransportRequest.DestinationLatitude,

                    DestinationLongitude =
                        t.TransportRequest.DestinationLongitude,


                    // ====================================================
                    // REQUEST INFORMATION
                    // ====================================================

                    RequestReason =
                        t.TransportRequest.RequestReason,

                    RequestedForDate =
                        t.TransportRequest.RequestedForDate,

                    RequestedForTime =
                        t.TransportRequest.RequestedForTime,

                    RequestedAt =
                        t.TransportRequest.RequestedAt,


                    // ====================================================
                    // ROUTE INFORMATION
                    // ====================================================

                    TravelDistance =
                        t.TransportRequest.TravelDistance,

                    TravelTime =
                        t.TransportRequest.TravelTime
                })

                .ToListAsync();


            // ========================================================
            // RETURN TRIP HISTORY
            // ========================================================

            return Ok(trips);
        }
        // ============================================================
        // GET SINGLE TRANSPORT REQUEST
        //
        // DRIVER CAN VIEW ONLY A REQUEST ASSIGNED TO THEM
        // ============================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("details/{requestId}")]
        public async Task<IActionResult> GetRequestDetails(
            int requestId)
        {
            // ========================================================
            // VALIDATE REQUEST ID
            // ========================================================

            if (requestId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid requestId is required."
                });
            }

            // ========================================================
            // GET LOGGED-IN USER FROM JWT
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }

            // ========================================================
            // VERIFY LOGGED-IN USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }

            // ========================================================
            // GET ONLY THE REQUEST ASSIGNED TO THIS LOGGED-IN DRIVER
            // ========================================================

            var request = await _context.TransportRequests
                .AsNoTracking()
                .Where(r =>
                    r.RequestId == requestId &&
                    r.AssignedToId == driver.Id
                )
                .Select(r => new TransportRequestDto
                {
                    // ====================================================
                    // REQUEST DETAILS
                    // ====================================================

                    RequestId = r.RequestId,
                    RequestedForId = r.RequestedForId,
                    RequestReason = r.RequestReason,
                    RequestedForDate = r.RequestedForDate,
                    RequestedForTime = r.RequestedForTime,

                    PickUpAddress = r.PickUpAddress,
                    DestinationAddress = r.DestinationAddress,

                    PickUpLatitude = r.PickUpLatitude,
                    PickUpLongitude = r.PickUpLongitude,

                    DestinationLatitude = r.DestinationLatitude,
                    DestinationLongitude = r.DestinationLongitude,

                    RequestedAt = r.RequestedAt,

                    TravelDistance = r.TravelDistance,
                    TravelTime = r.TravelTime,

                    AssignedToId = r.AssignedToId,
                    VehicleId = r.VehicleId,

                    // ====================================================
                    // REQUEST STATUS (EXISTING REQUEST STATUS)
                    // ====================================================

                    RequestStatus = r.Status,

                    // ====================================================
                    // TRIP DETAILS (IF A TRIP EXISTS)
                    // ====================================================

                    TripId = r.Trip != null
        ? r.Trip.TripId
        : null,

                    TripStatus = r.Trip != null
        ? r.Trip.Status
        : null,

                    // ====================================================
                    // RESIDENT DETAILS
                    // ====================================================

                    ResidentFirstName = r.Resident.FirstName,
                    ResidentLastName = r.Resident.LastName,
                    ResidentProfilePicture = r.Resident.ProfilePicture,
                    RoomNumber = r.Resident.RoomNumber
                })
                .FirstOrDefaultAsync();

            // ========================================================
            // REQUEST NOT FOUND OR DOES NOT BELONG TO THIS DRIVER
            // ========================================================

            if (request == null)
            {
                return NotFound(new
                {
                    message =
                        "Transport request not found or is not assigned to the logged-in driver."
                });
            }

            return Ok(request);
        }

        // ========================================================
        // APPROVE TRANSPORT REQUEST
        // ========================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("approve_request")]
        public async Task<IActionResult> ApproveRequest(
            [FromQuery] int requestId)
        {
            // ========================================================
            // VALIDATE REQUEST ID
            // ========================================================

            if (requestId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid requestId is required."
                });
            }


            // ========================================================
            // GET LOGGED-IN USER FROM JWT
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY LOGGED-IN USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // FIND REQUEST ASSIGNED TO THIS DRIVER
            // INCLUDE TRIP SO WE CAN CHECK WHETHER ONE ALREADY EXISTS
            // ========================================================

            var request = await _context.TransportRequests
                .Include(r => r.Trip)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.AssignedToId == driver.Id);


            if (request == null)
            {
                return NotFound(new
                {
                    message =
                        "Transport request not found or is not assigned to you."
                });
            }


            // ========================================================
            // ONLY DRIVER_ASSIGNED REQUESTS CAN BE APPROVED
            // ========================================================

            if (request.Status != RequestStatus.Driver_Assigned)
            {
                return BadRequest(new
                {
                    message =
                        $"This transport request cannot be approved because " +
                        $"its current status is {request.Status}."
                });
            }


            // ========================================================
            // APPROVE TRANSPORT REQUEST
            // ========================================================

            request.Status = RequestStatus.Driver_Approved;


            // ========================================================
            // CREATE TRIP
            // ========================================================

            /*
             * When the driver approves the request, the trip is created.
             *
             * However, the trip has NOT started yet.
             *
             * The driver will explicitly press "Start Trip" later.
             */

            if (request.Trip == null)
            {
                var trip = new Trip
                {
                    RequestId = request.RequestId,

                    Status = TripStatus.Not_Started,

                    CurrentLatitude = null,

                    CurrentLongitude = null,

                    StartedAt = null,

                    EndedAt = null
                };


                _context.Trips.Add(trip);
            }


            // ========================================================
            // SAVE CHANGES
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // GET CREATED TRIP
            // ========================================================

            var createdTrip = await _context.Trips
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.RequestId == request.RequestId);


            // ========================================================
            // RETURN SUCCESS
            // ========================================================

            return Ok(new
            {
                message =
                    "Transport request approved successfully. Trip has been created and is ready to start.",

                requestId = request.RequestId,

                assignedDriverId = request.AssignedToId,

                requestStatus = request.Status.ToString(),

                tripId = createdTrip?.TripId,

                tripStatus = createdTrip?.Status.ToString()
            });
        }

        // ========================================================
        // START TRIP
        //
        // This will:
        //
        // 1. Verify requestId
        // 2. Verify logged-in user
        // 3. Verify user is a driver
        // 4. Find the assigned transport request
        // 5. Verify request is approved
        // 6. Verify a vehicle is assigned
        // 7. Verify trip exists
        // 8. Verify driver has no OTHER active trip
        // 9. Verify current trip can be started
        // 10. Start trip
        // 11. Save changes
        // ========================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("start_trip")]
        public async Task<IActionResult> StartTrip(
            [FromQuery] int requestId)
        {
            // ========================================================
            // VALIDATE REQUEST ID
            // ========================================================

            if (requestId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid requestId is required."
                });
            }


            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // FIND TRANSPORT REQUEST
            // ========================================================

            var request = await _context.TransportRequests
                .Include(r => r.Trip)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.AssignedToId == driver.Id);

            if (request == null)
            {
                return NotFound(new
                {
                    message =
                        "Transport request not found or is not assigned to you."
                });
            }


            // ========================================================
            // VERIFY REQUEST WAS APPROVED
            // ========================================================

            if (request.Status != RequestStatus.Driver_Approved)
            {
                return BadRequest(new
                {
                    message =
                        $"This trip cannot be started because the request status is {request.Status}."
                });
            }


            // ========================================================
            // VERIFY VEHICLE IS ASSIGNED
            // ========================================================

            if (!request.VehicleId.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "This trip cannot be started because no vehicle has been assigned to this request.",

                    requestId =
                        request.RequestId
                });
            }


            // ========================================================
            // VERIFY ASSIGNED VEHICLE EXISTS
            // ========================================================

            if (request.Vehicle == null)
            {
                return BadRequest(new
                {
                    message =
                        "This trip cannot be started because the assigned vehicle could not be found.",

                    requestId =
                        request.RequestId,

                    vehicleId =
                        request.VehicleId
                });
            }


            // ========================================================
            // VERIFY TRIP EXISTS
            // ========================================================

            if (request.Trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Trip not found. The transport request may not have been approved correctly."
                });
            }


            // ========================================================
            // CHECK IF DRIVER ALREADY HAS ANOTHER STARTED TRIP
            //
            // IMPORTANT:
            // Exclude the current request so that this check only
            // detects ANOTHER active trip belonging to this driver.
            // ========================================================

            var activeTrip = await _context.TransportRequests
                .Include(r => r.Trip)
                .FirstOrDefaultAsync(r =>
                    r.AssignedToId == driver.Id &&
                    r.RequestId != request.RequestId &&
                    r.Trip != null &&
                    r.Trip.Status == TripStatus.Started);

            if (activeTrip != null)
            {
                return BadRequest(new
                {
                    message =
                        "You cannot start this trip because you already have another trip in progress. Please complete your current trip first.",

                    currentRequestId =
                        activeTrip.RequestId,

                    currentTripId =
                        activeTrip.Trip!.TripId,

                    currentTripStatus =
                        activeTrip.Trip.Status.ToString(),

                    currentTripStartedAt =
                        activeTrip.Trip.StartedAt
                });
            }


            // ========================================================
            // CHECK CURRENT TRIP STATUS
            // ========================================================

            if (request.Trip.Status == TripStatus.Started)
            {
                return BadRequest(new
                {
                    message =
                        "This trip has already been started.",

                    requestId =
                        request.RequestId,

                    tripId =
                        request.Trip.TripId,

                    tripStatus =
                        request.Trip.Status.ToString(),

                    startedAt =
                        request.Trip.StartedAt
                });
            }


            // ========================================================
            // CHECK IF TRIP HAS ALREADY ENDED
            // ========================================================

            if (request.Trip.Status == TripStatus.Ended)
            {
                return BadRequest(new
                {
                    message =
                        "This trip has already ended and cannot be started again.",

                    requestId =
                        request.RequestId,

                    tripId =
                        request.Trip.TripId,

                    tripStatus =
                        request.Trip.Status.ToString(),

                    endedAt =
                        request.Trip.EndedAt
                });
            }


            // ========================================================
            // START TRIP
            // ========================================================

            request.Trip.Status =
                TripStatus.Started;

            request.Trip.StartedAt =
                DateTime.Now;

            request.Trip.EndedAt =
                null;


            // ========================================================
            // SAVE CHANGES
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // RETURN SUCCESS
            // ========================================================

            return Ok(new
            {
                message =
                    "Trip started successfully.",

                requestId =
                    request.RequestId,

                tripId =
                    request.Trip.TripId,

                vehicleId =
                    request.VehicleId,

                requestStatus =
                    request.Status.ToString(),

                tripStatus =
                    request.Trip.Status.ToString(),

                startedAt =
                    request.Trip.StartedAt
            });
        }

        // ========================================================
        // END TRIP
        //
        // This will:
        //
        // 1. Verify the driver
        // 2. Find the assigned request
        // 3. Find the vehicle assigned to the request
        // 4. Verify the trip exists
        // 5. Verify trip is currently started
        // 6. Mark Trip.Status = Ended
        // 7. Set EndedAt
        // 8. Mark Request.Status = Completed
        // 9. Mark Vehicle.Status = Available
        // 10. Save everything
        // ========================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("end_trip")]
        public async Task<IActionResult> EndTrip(
            [FromQuery] int requestId)
        {
            // ========================================================
            // VALIDATE REQUEST ID
            // ========================================================

            if (requestId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid requestId is required."
                });
            }


            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // FIND ASSIGNED TRANSPORT REQUEST
            //
            // ALSO LOAD:
            // - Trip
            // - Vehicle
            // ========================================================

            var request = await _context.TransportRequests
                .Include(r => r.Trip)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.AssignedToId == driver.Id);

            if (request == null)
            {
                return NotFound(new
                {
                    message =
                        "Transport request not found or is not assigned to you."
                });
            }


            // ========================================================
            // VERIFY TRIP EXISTS
            // ========================================================

            if (request.Trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Trip not found for this transport request."
                });
            }


            // ========================================================
            // CHECK IF ALREADY COMPLETED
            // ========================================================

            if (request.Trip.Status == TripStatus.Ended ||
                request.Status == RequestStatus.Completed)
            {
                return BadRequest(new
                {
                    message =
                        "This trip has already been completed.",

                    requestId =
                        request.RequestId,

                    tripId =
                        request.Trip.TripId,

                    vehicleId =
                        request.VehicleId,

                    requestStatus =
                        request.Status.ToString(),

                    tripStatus =
                        request.Trip.Status.ToString(),

                    endedAt =
                        request.Trip.EndedAt
                });
            }


            // ========================================================
            // VERIFY TRIP WAS STARTED
            // ========================================================

            if (request.Trip.Status != TripStatus.Started)
            {
                return BadRequest(new
                {
                    message =
                        "This trip cannot be ended because it has not been started.",

                    tripStatus =
                        request.Trip.Status.ToString()
                });
            }


            // ========================================================
            // VERIFY VEHICLE EXISTS
            // ========================================================

            if (request.Vehicle == null)
            {
                return BadRequest(new
                {
                    message =
                        "No vehicle is associated with this transport request.",

                    requestId =
                        request.RequestId,

                    vehicleId =
                        request.VehicleId
                });
            }


            // ========================================================
            // END TRIP
            // ========================================================

            request.Trip.Status =
                TripStatus.Ended;

            request.Trip.EndedAt =
                DateTime.Now;


            // ========================================================
            // MARK TRANSPORT REQUEST AS COMPLETED
            // ========================================================

            request.Status =
                RequestStatus.Completed;


            // ========================================================
            // RELEASE VEHICLE
            //
            // The vehicle was associated with this request.
            // Once the trip is completed, make it available again.
            // ========================================================

            request.Vehicle.Status =
                VehicleStatus.Available;


            // ========================================================
            // SAVE EVERYTHING
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // RETURN SUCCESS
            // ========================================================

            return Ok(new
            {
                message =
                    "Trip ended successfully, transport request completed, and vehicle is now available.",

                requestId =
                    request.RequestId,

                tripId =
                    request.Trip.TripId,

                vehicleId =
                    request.Vehicle.VehicleId,

                requestStatus =
                    request.Status.ToString(),

                tripStatus =
                    request.Trip.Status.ToString(),

                vehicleStatus =
                    request.Vehicle.Status.ToString(),

                currentLatitude =
                    request.Trip.CurrentLatitude,

                currentLongitude =
                    request.Trip.CurrentLongitude,

                startedAt =
                    request.Trip.StartedAt,

                endedAt =
                    request.Trip.EndedAt
            });
        }


        // ========================================================
        // UPDATE DRIVER CURRENT LOCATION
        // ========================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPut("update_location")]
        public async Task<IActionResult> UpdateLocation(
            [FromBody] UpdateTripLocationRequest model)
        {
            // ========================================================
            // VALIDATE MODEL
            // ========================================================

            if (model == null)
            {
                return BadRequest(new
                {
                    message =
                        "Location data is required."
                });
            }


            if (model.TripId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "A valid tripId is required."
                });
            }


            // ========================================================
            // VALIDATE LATITUDE
            // ========================================================

            if (model.CurrentLatitude < -90 ||
                model.CurrentLatitude > 90)
            {
                return BadRequest(new
                {
                    message =
                        "Current latitude must be between -90 and 90."
                });
            }


            // ========================================================
            // VALIDATE LONGITUDE
            // ========================================================

            if (model.CurrentLongitude < -180 ||
                model.CurrentLongitude > 180)
            {
                return BadRequest(new
                {
                    message =
                        "Current longitude must be between -180 and 180."
                });
            }


            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // FIND TRIP
            //
            // We also verify that this trip belongs to a request
            // assigned to the logged-in driver.
            // ========================================================

            var trip = await _context.Trips
                .Include(t => t.TransportRequest)
                .FirstOrDefaultAsync(t =>
                    t.TripId == model.TripId &&
                    t.TransportRequest.AssignedToId == driver.Id);

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Trip not found or this trip is not assigned to you."
                });
            }


            // ========================================================
            // ONLY STARTED TRIPS CAN UPDATE LOCATION
            // ========================================================

            if (trip.Status != TripStatus.Started)
            {
                return BadRequest(new
                {
                    message =
                        "Driver location can only be updated while the trip is started.",

                    tripStatus =
                        trip.Status.ToString()
                });
            }


            // ========================================================
            // UPDATE LOCATION
            // ========================================================

            trip.CurrentLatitude =
                model.CurrentLatitude;

            trip.CurrentLongitude =
                model.CurrentLongitude;


            // ========================================================
            // SAVE
            // ========================================================

            await _context.SaveChangesAsync();


            // ========================================================
            // RETURN UPDATED LOCATION
            // ========================================================

            return Ok(new
            {
                message =
                    "Driver location updated successfully.",

                tripId =
                    trip.TripId,

                requestId =
                    trip.RequestId,

                tripStatus =
                    trip.Status.ToString(),

                currentLatitude =
                    trip.CurrentLatitude,

                currentLongitude =
                    trip.CurrentLongitude,

                updatedAt =
                    DateTime.Now
            });
        }


        // ========================================================
        // GET DRIVER CURRENT LOCATION
        // ========================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("current_location")]
        public async Task<IActionResult> GetCurrentLocation(
            [FromQuery] int tripId)
        {
            // ========================================================
            // VALIDATE TRIP ID
            // ========================================================

            if (tripId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "A valid tripId is required."
                });
            }


            // ========================================================
            // GET LOGGED-IN USER
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message =
                        "User not found or authentication token is invalid."
                });
            }


            // ========================================================
            // VERIFY DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }


            // ========================================================
            // FIND TRIP
            // ========================================================

            var trip = await _context.Trips
                .Include(t => t.TransportRequest)
                .FirstOrDefaultAsync(t =>
                    t.TripId == tripId &&
                    t.TransportRequest.AssignedToId == driver.Id);

            if (trip == null)
            {
                return NotFound(new
                {
                    message =
                        "Trip not found or this trip is not assigned to you."
                });
            }


            // ========================================================
            // RETURN CURRENT LOCATION
            // ========================================================

            return Ok(new
            {
                message =
                    "Current driver location retrieved successfully.",

                tripId =
                    trip.TripId,

                requestId =
                    trip.RequestId,

                tripStatus =
                    trip.Status.ToString(),

                currentLatitude =
                    trip.CurrentLatitude,

                currentLongitude =
                    trip.CurrentLongitude,

                startedAt =
                    trip.StartedAt,

                endedAt =
                    trip.EndedAt
            });
        }
    

        // ========================================================
        // REJECT TRANSPORT REQUEST
        // ========================================================

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("reject_request")]
        public async Task<IActionResult> RejectRequest(
            [FromQuery] int requestId)
        {
            // ========================================================
            // VALIDATE REQUEST ID
            // ========================================================

            if (requestId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid requestId is required."
                });
            }

            // ========================================================
            // GET LOGGED-IN USER FROM JWT
            // ========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found or authentication token is invalid."
                });
            }

            // ========================================================
            // VERIFY THAT THE LOGGED-IN USER IS A DRIVER
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == user.Id);

            if (driver == null)
            {
                return Forbid();
            }

            // ========================================================
            // FIND REQUEST ASSIGNED SPECIFICALLY TO THIS DRIVER
            // ========================================================

            var request = await _context.TransportRequests
                .FirstOrDefaultAsync(r =>
                    r.RequestId == requestId &&
                    r.AssignedToId == driver.Id);

            if (request == null)
            {
                return NotFound(new
                {
                    message = "Transport request not found or is not assigned to you."
                });
            }

            // ========================================================
            // ONLY ASSIGNED REQUESTS CAN BE REJECTED
            // ========================================================

            if (request.Status != RequestStatus.Driver_Assigned)
            {
                return BadRequest(new
                {
                    message =
                        $"This transport request cannot be rejected because " +
                        $"its current status is {request.Status}."
                });
            }

            // ========================================================
            // REJECT REQUEST
            // ========================================================

            request.Status = RequestStatus.Driver_Rejected;

            // ========================================================
            // REMOVE DRIVER ASSIGNMENT
            // ========================================================

            request.AssignedToId = null;

            // ========================================================
            // SAVE CHANGES
            // ========================================================

            await _context.SaveChangesAsync();

            // ========================================================
            // RETURN SUCCESS
            // ========================================================

            return Ok(new
            {
                message = "Transport request rejected successfully.",
                requestId = request.RequestId,
                assignedDriverId = request.AssignedToId,
                status = request.Status.ToString()
            });
        }
    }
}