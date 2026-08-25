namespace Raphael.Shared.Catalog.BusinessEvents;

public static class BusinessEventCatalog
{
    public static IReadOnlyList<BusinessEventCatalogItem> Events =>
    [
        #region Scheduling Domain
        // =====================================================
        // Scheduling Domain
        // =====================================================

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "TRIP_LIFECYCLE",
            GroupName = "Trip Lifecycle",
            GroupDescription = "Events generated during trip creation and lifecycle.",

            EventCode = "TRIP_REQUESTED",
            EventName = "Trip Requested",
            EventDescription = "A trip request was created.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "TRIP_LIFECYCLE",
            GroupName = "Trip Lifecycle",
            GroupDescription = "Events generated during trip creation and lifecycle.",

            EventCode = "TRIP_CREATED",
            EventName = "Trip Created",
            EventDescription = "A trip was created.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "TRIP_LIFECYCLE",
            GroupName = "Trip Lifecycle",
            GroupDescription = "Events generated during trip creation and lifecycle.",

            EventCode = "TRIP_SCHEDULED",
            EventName = "Trip Scheduled",
            EventDescription = "A trip was scheduled successfully.",
            Source = "ScheduleService"
        },


        // =====================================================
        // Assignment
        // =====================================================

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ASSIGNMENT",
            GroupName = "Assignment",
            GroupDescription = "Events related to assigning trips.",

            EventCode = "TRIP_ASSIGNED",
            EventName = "Trip Assigned",
            EventDescription = "A trip was assigned.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ASSIGNMENT",
            GroupName = "Assignment",
            GroupDescription = "Events related to assigning trips.",

            EventCode = "TRIP_REASSIGNED",
            EventName = "Trip Reassigned",
            EventDescription = "A trip was reassigned.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ASSIGNMENT",
            GroupName = "Assignment",
            GroupDescription = "Events related to assigning trips.",

            EventCode = "TRIP_UNASSIGNED",
            EventName = "Trip Unassigned",
            EventDescription = "A trip assignment was removed.",
            Source = "ScheduleService"
        },


        // =====================================================
        // Administration
        // =====================================================

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_MODIFIED",
            EventName = "Trip Modified",
            EventDescription = "Trip information was modified.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_CANCELLED",
            EventName = "Trip Cancelled",
            EventDescription = "A trip was cancelled.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_REACTIVATED",
            EventName = "Trip Reactivated",
            EventDescription = "A cancelled trip was put back in service.",
            Source = "TripService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_RESCHEDULED",
            EventName = "Trip Rescheduled",
            EventDescription = "A trip date or time was changed.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_CONFIRMED",
            EventName = "Trip Confirmed",
            EventDescription = "A trip was confirmed.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_REJECTED",
            EventName = "Trip Rejected",
            EventDescription = "A trip was rejected.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_DUPLICATED",
            EventName = "Trip Duplicated",
            EventDescription = "A trip was duplicated.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_SPLIT",
            EventName = "Trip Split",
            EventDescription = "A trip was split into multiple trips.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_MERGED",
            EventName = "Trip Merged",
            EventDescription = "Multiple trips were merged.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_LOCKED",
            EventName = "Trip Locked",
            EventDescription = "A trip was locked.",
            Source = "ScheduleService"
        },

        new()
        {
            CategoryCode = "SCHEDULING",
            CategoryName = "Scheduling",
            CategoryDescription = "Events related to trip scheduling and lifecycle.",

            GroupCode = "ADMINISTRATION",
            GroupName = "Administration",
            GroupDescription = "Administrative trip events.",

            EventCode = "TRIP_UNLOCKED",
            EventName = "Trip Unlocked",
            EventDescription = "A trip was unlocked.",
            Source = "ScheduleService"
        },

        #endregion

        #region Driver Operations Domain

        // =====================================================
        // Driver Operations Domain
        // =====================================================

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "DRIVER_ACTIVITY",
            GroupName = "Driver Activity",
            GroupDescription = "Events related to driver status and actions.",

            EventCode = "DRIVER_ASSIGNED",
            EventName = "Driver Assigned",
            EventDescription = "A driver was assigned to an operation.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "DRIVER_ACTIVITY",
            GroupName = "Driver Activity",
            GroupDescription = "Events related to driver status and actions.",

            EventCode = "DRIVER_UNASSIGNED",
            EventName = "Driver Unassigned",
            EventDescription = "A driver assignment was removed.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "DRIVER_ACTIVITY",
            GroupName = "Driver Activity",
            GroupDescription = "Events related to driver status and actions.",

            EventCode = "DRIVER_LOGGED_IN",
            EventName = "Driver Logged In",
            EventDescription = "Driver logged into the application.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "DRIVER_ACTIVITY",
            GroupName = "Driver Activity",
            GroupDescription = "Events related to driver status and actions.",

            EventCode = "DRIVER_LOGGED_OUT",
            EventName = "Driver Logged Out",
            EventDescription = "Driver logged out from the application.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "SHIFT_MANAGEMENT",
            GroupName = "Shift Management",
            GroupDescription = "Events related to driver shifts.",

            EventCode = "DRIVER_STARTED_SHIFT",
            EventName = "Driver Started Shift",
            EventDescription = "Driver started working shift.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "SHIFT_MANAGEMENT",
            GroupName = "Shift Management",
            GroupDescription = "Events related to driver shifts.",

            EventCode = "DRIVER_ENDED_SHIFT",
            EventName = "Driver Ended Shift",
            EventDescription = "Driver ended working shift.",
            Source = "DriverService"
        },


        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "ROUTE_OPERATIONS",
            GroupName = "Route Operations",
            GroupDescription = "Events related to driver routes.",

            EventCode = "DRIVER_ACCEPTED_ROUTE",
            EventName = "Driver Accepted Route",
            EventDescription = "Driver accepted assigned route.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "ROUTE_OPERATIONS",
            GroupName = "Route Operations",
            GroupDescription = "Events related to driver routes.",

            EventCode = "DRIVER_REJECTED_ROUTE",
            EventName = "Driver Rejected Route",
            EventDescription = "Driver rejected assigned route.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "ROUTE_OPERATIONS",
            GroupName = "Route Operations",
            GroupDescription = "Events related to driver routes.",

            EventCode = "DRIVER_ROUTE_UPDATED",
            EventName = "Driver Route Updated",
            EventDescription = "Driver route information was updated.",
            Source = "DriverService"
        },


        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_STARTED_TRIP",
            EventName = "Driver Started Trip",
            EventDescription = "Driver started the trip.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_ARRIVED_PICKUP",
            EventName = "Driver Arrived Pickup",
            EventDescription = "Driver arrived at pickup location.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_PICKED_UP_PASSENGER",
            EventName = "Driver Picked Up Passenger",
            EventDescription = "Passenger was picked up.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_DEPARTED_PICKUP",
            EventName = "Driver Departed Pickup",
            EventDescription = "Driver departed pickup location.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_ARRIVED_DROPOFF",
            EventName = "Driver Arrived Dropoff",
            EventDescription = "Driver arrived at dropoff location.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_COMPLETED_TRIP",
            EventName = "Driver Completed Trip",
            EventDescription = "Driver completed the trip.",
            Source = "DriverService"
        },

        new()
        {
            CategoryCode = "DRIVER_OPERATIONS",
            CategoryName = "Driver Operations",
            CategoryDescription = "Events generated during driver operations.",

            GroupCode = "TRIP_EXECUTION",
            GroupName = "Trip Execution",
            GroupDescription = "Events generated while performing a trip.",

            EventCode = "DRIVER_CANCELLED_TRIP",
            EventName = "Driver Cancelled Trip",
            EventDescription = "Driver cancelled a trip.",
            Source = "DriverService"
        },

        #endregion

        #region Rider Domain

        // =====================================================
        // Rider Domain
        // =====================================================

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "RIDER_ACTIVITY",
            GroupName = "Rider Activity",
            GroupDescription = "Events related to rider interactions.",

            EventCode = "RIDER_REGISTERED",
            EventName = "Rider Registered",
            EventDescription = "A rider was registered.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "RIDER_ACTIVITY",
            GroupName = "Rider Activity",
            GroupDescription = "Events related to rider interactions.",

            EventCode = "RIDER_CHECKED_IN",
            EventName = "Rider Checked In",
            EventDescription = "A rider checked in for a trip.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "RIDER_ACTIVITY",
            GroupName = "Rider Activity",
            GroupDescription = "Events related to rider interactions.",

            EventCode = "RIDER_CHECKED_OUT",
            EventName = "Rider Checked Out",
            EventDescription = "A rider checked out after a trip.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "TRIP_INTERACTION",
            GroupName = "Trip Interaction",
            GroupDescription = "Events related to rider trip decisions.",

            EventCode = "RIDER_CONFIRMED_TRIP",
            EventName = "Rider Confirmed Trip",
            EventDescription = "Rider confirmed participation in a trip.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "TRIP_INTERACTION",
            GroupName = "Trip Interaction",
            GroupDescription = "Events related to rider trip decisions.",

            EventCode = "RIDER_CANCELLED_TRIP",
            EventName = "Rider Cancelled Trip",
            EventDescription = "Rider cancelled a trip.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "TRIP_INTERACTION",
            GroupName = "Trip Interaction",
            GroupDescription = "Events related to rider trip decisions.",

            EventCode = "RIDER_NO_SHOW",
            EventName = "Rider No Show",
            EventDescription = "Rider did not appear for the scheduled trip.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "FEEDBACK",
            GroupName = "Feedback",
            GroupDescription = "Events related to ratings and feedback.",

            EventCode = "RIDER_RATED_TRIP",
            EventName = "Rider Rated Trip",
            EventDescription = "Rider rated a completed trip.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "FEEDBACK",
            GroupName = "Feedback",
            GroupDescription = "Events related to ratings and feedback.",

            EventCode = "RIDER_RATED_DRIVER",
            EventName = "Rider Rated Driver",
            EventDescription = "Rider rated the driver.",
            Source = "RiderService"
        },

        new()
        {
            CategoryCode = "RIDER",
            CategoryName = "Rider",
            CategoryDescription = "Events generated by passenger actions and lifecycle.",

            GroupCode = "SUPPORT",
            GroupName = "Support",
            GroupDescription = "Events related to rider support requests.",

            EventCode = "RIDER_REQUESTED_SUPPORT",
            EventName = "Rider Requested Support",
            EventDescription = "Rider requested assistance.",
            Source = "RiderService"
        },

        #endregion

        #region GPS Domain

        // =====================================================
        // GPS Domain
        // =====================================================

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "LOCATION_TRACKING",
            GroupName = "Location Tracking",
            GroupDescription = "Events related to vehicle location changes.",

            EventCode = "VEHICLE_LOCATION_UPDATED",
            EventName = "Vehicle Location Updated",
            EventDescription = "Vehicle location was updated.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "VEHICLE_STATUS",
            GroupName = "Vehicle Status",
            GroupDescription = "Events related to vehicle movement status.",

            EventCode = "VEHICLE_STOPPED",
            EventName = "Vehicle Stopped",
            EventDescription = "Vehicle stopped movement.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "VEHICLE_STATUS",
            GroupName = "Vehicle Status",
            GroupDescription = "Events related to vehicle movement status.",

            EventCode = "VEHICLE_MOVING",
            EventName = "Vehicle Moving",
            EventDescription = "Vehicle started moving.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "GEOFENCE",
            GroupName = "Geofence",
            GroupDescription = "Events related to geographic boundaries.",

            EventCode = "VEHICLE_ENTERED_GEOFENCE",
            EventName = "Vehicle Entered Geofence",
            EventDescription = "Vehicle entered a configured geofence.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "GEOFENCE",
            GroupName = "Geofence",
            GroupDescription = "Events related to geographic boundaries.",

            EventCode = "VEHICLE_EXITED_GEOFENCE",
            EventName = "Vehicle Exited Geofence",
            EventDescription = "Vehicle exited a configured geofence.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "SIGNAL",
            GroupName = "GPS Signal",
            GroupDescription = "Events related to GPS availability.",

            EventCode = "GPS_LOST",
            EventName = "GPS Lost",
            EventDescription = "Vehicle GPS signal was lost.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "SIGNAL",
            GroupName = "GPS Signal",
            GroupDescription = "Events related to GPS availability.",

            EventCode = "GPS_RECOVERED",
            EventName = "GPS Recovered",
            EventDescription = "Vehicle GPS signal was recovered.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "SAFETY",
            GroupName = "Safety Monitoring",
            GroupDescription = "Events related to driving safety.",

            EventCode = "SPEED_LIMIT_EXCEEDED",
            EventName = "Speed Limit Exceeded",
            EventDescription = "Vehicle exceeded configured speed limit.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "ROUTE_MONITORING",
            GroupName = "Route Monitoring",
            GroupDescription = "Events related to route compliance.",

            EventCode = "ROUTE_DEVIATION_DETECTED",
            EventName = "Route Deviation Detected",
            EventDescription = "Vehicle deviated from assigned route.",
            Source = "GpsService"
        },

        new()
        {
            CategoryCode = "GPS",
            CategoryName = "GPS",
            CategoryDescription = "Events generated by vehicle tracking and GPS monitoring.",

            GroupCode = "ETA",
            GroupName = "ETA Tracking",
            GroupDescription = "Events related to estimated arrival time.",

            EventCode = "ETA_UPDATED",
            EventName = "ETA Updated",
            EventDescription = "Estimated arrival time changed.",
            Source = "GpsService"
        },

        #endregion

        #region Route Domain

        // =====================================================
        // Route Domain
        // =====================================================

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_MANAGEMENT",
            GroupName = "Route Management",
            GroupDescription = "Events related to route lifecycle.",

            EventCode = "ROUTE_CREATED",
            EventName = "Route Created",
            EventDescription = "A route was created.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_MANAGEMENT",
            GroupName = "Route Management",
            GroupDescription = "Events related to route lifecycle.",

            EventCode = "ROUTE_PUBLISHED",
            EventName = "Route Published",
            EventDescription = "A route was published and made available.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_MANAGEMENT",
            GroupName = "Route Management",
            GroupDescription = "Events related to route lifecycle.",

            EventCode = "ROUTE_MODIFIED",
            EventName = "Route Modified",
            EventDescription = "A route was modified.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_OPTIMIZATION",
            GroupName = "Route Optimization",
            GroupDescription = "Events related to route optimization.",

            EventCode = "ROUTE_OPTIMIZED",
            EventName = "Route Optimized",
            EventDescription = "A route was optimized.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_EXECUTION",
            GroupName = "Route Execution",
            GroupDescription = "Events generated during route execution.",

            EventCode = "ROUTE_STARTED",
            EventName = "Route Started",
            EventDescription = "A route started execution.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_EXECUTION",
            GroupName = "Route Execution",
            GroupDescription = "Events generated during route execution.",

            EventCode = "ROUTE_COMPLETED",
            EventName = "Route Completed",
            EventDescription = "A route was completed.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_EXECUTION",
            GroupName = "Route Execution",
            GroupDescription = "Events generated during route execution.",

            EventCode = "ROUTE_CANCELLED",
            EventName = "Route Cancelled",
            EventDescription = "A route was cancelled.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_STOPS",
            GroupName = "Route Stops",
            GroupDescription = "Events related to route stops.",

            EventCode = "STOP_ADDED",
            EventName = "Stop Added",
            EventDescription = "A stop was added to a route.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_STOPS",
            GroupName = "Route Stops",
            GroupDescription = "Events related to route stops.",

            EventCode = "STOP_REMOVED",
            EventName = "Stop Removed",
            EventDescription = "A stop was removed from a route.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_STOPS",
            GroupName = "Route Stops",
            GroupDescription = "Events related to route stops.",

            EventCode = "STOP_MODIFIED",
            EventName = "Stop Modified",
            EventDescription = "A route stop was modified.",
            Source = "RouteService"
        },

        new()
        {
            CategoryCode = "ROUTE",
            CategoryName = "Route",
            CategoryDescription = "Events generated during route creation and management.",

            GroupCode = "ROUTE_STOPS",
            GroupName = "Route Stops",
            GroupDescription = "Events related to route stops.",

            EventCode = "STOP_REORDERED",
            EventName = "Stop Reordered",
            EventDescription = "Route stop order was changed.",
            Source = "RouteService"
        },

        #endregion

        #region Will Call Domain

        // =====================================================
        // Will Call Domain
        // =====================================================

        new()
        {
            CategoryCode = "WILL_CALL",
            CategoryName = "Will Call",
            CategoryDescription = "Events generated by the Will Call workflow.",

            GroupCode = "WILL_CALL_LIFECYCLE",
            GroupName = "Will Call Lifecycle",
            GroupDescription = "Events related to Will Call creation and status changes.",

            EventCode = "WILL_CALL_CREATED",
            EventName = "Will Call Created",
            EventDescription = "A trip became a Will Call and now waits for the patient to say they are ready.",
            Source = "TripService"
        },

        new()
        {
            CategoryCode = "WILL_CALL",
            CategoryName = "Will Call",
            CategoryDescription = "Events generated by the Will Call workflow.",

            GroupCode = "WILL_CALL_LIFECYCLE",
            GroupName = "Will Call Lifecycle",
            GroupDescription = "Events related to Will Call creation and status changes.",

            EventCode = "WILL_CALL_ACTIVATED",
            EventName = "Will Call Activated",
            EventDescription = "A Will Call was activated.",
            Source = "WillCallService"
        },

        new()
        {
            CategoryCode = "WILL_CALL",
            CategoryName = "Will Call",
            CategoryDescription = "Events generated by the Will Call workflow.",

            GroupCode = "WILL_CALL_LIFECYCLE",
            GroupName = "Will Call Lifecycle",
            GroupDescription = "Events related to Will Call creation and status changes.",

            EventCode = "WILL_CALL_ACKNOWLEDGED",
            EventName = "Will Call Acknowledged",
            EventDescription = "A Will Call notification was acknowledged by a recipient.",
            Source = "NotificationService"
        },

        new()
        {
            CategoryCode = "WILL_CALL",
            CategoryName = "Will Call",
            CategoryDescription = "Events generated by the Will Call workflow.",

            GroupCode = "WILL_CALL_LIFECYCLE",
            GroupName = "Will Call Lifecycle",
            GroupDescription = "Events related to Will Call creation and status changes.",

            EventCode = "WILL_CALL_EXPIRED",
            EventName = "Will Call Expired",
            EventDescription = "A Will Call request expired.",
            Source = "WillCallService"
        },

        new()
        {
            CategoryCode = "WILL_CALL",
            CategoryName = "Will Call",
            CategoryDescription = "Events generated by the Will Call workflow.",

            GroupCode = "WILL_CALL_LIFECYCLE",
            GroupName = "Will Call Lifecycle",
            GroupDescription = "Events related to Will Call creation and status changes.",

            EventCode = "WILL_CALL_CANCELLED",
            EventName = "Will Call Cancelled",
            EventDescription = "A Will Call request was cancelled.",
            Source = "WillCallService"
        },

        new()
        {
            CategoryCode = "WILL_CALL",
            CategoryName = "Will Call",
            CategoryDescription = "Events generated by the Will Call workflow.",

            GroupCode = "WILL_CALL_LIFECYCLE",
            GroupName = "Will Call Lifecycle",
            GroupDescription = "Events related to Will Call creation and status changes.",

            EventCode = "WILL_CALL_COMPLETED",
            EventName = "Will Call Completed",
            EventDescription = "A Will Call process was completed.",
            Source = "WillCallService"
        },

        #endregion

        #region Vehicle Domain

        // =====================================================
        // Vehicle Domain
        // =====================================================

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "VEHICLE_ASSIGNMENT",
            GroupName = "Vehicle Assignment",
            GroupDescription = "Events related to vehicle allocation.",

            EventCode = "VEHICLE_ASSIGNED",
            EventName = "Vehicle Assigned",
            EventDescription = "A vehicle was assigned.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "VEHICLE_ASSIGNMENT",
            GroupName = "Vehicle Assignment",
            GroupDescription = "Events related to vehicle allocation.",

            EventCode = "VEHICLE_UNASSIGNED",
            EventName = "Vehicle Unassigned",
            EventDescription = "A vehicle assignment was removed.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "VEHICLE_STATUS",
            GroupName = "Vehicle Status",
            GroupDescription = "Events related to vehicle operational status.",

            EventCode = "VEHICLE_CHANGED",
            EventName = "Vehicle Changed",
            EventDescription = "The vehicle assigned to a trip or route was changed.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "INSPECTION",
            GroupName = "Vehicle Inspection",
            GroupDescription = "Events related to vehicle inspections.",

            EventCode = "VEHICLE_INSPECTION_COMPLETED",
            EventName = "Vehicle Inspection Completed",
            EventDescription = "A vehicle inspection was completed successfully.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "INSPECTION",
            GroupName = "Vehicle Inspection",
            GroupDescription = "Events related to vehicle inspections.",

            EventCode = "VEHICLE_INSPECTION_FAILED",
            EventName = "Vehicle Inspection Failed",
            EventDescription = "A vehicle inspection failed.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "SERVICE_STATUS",
            GroupName = "Vehicle Service Status",
            GroupDescription = "Events related to vehicle availability.",

            EventCode = "VEHICLE_OUT_OF_SERVICE",
            EventName = "Vehicle Out Of Service",
            EventDescription = "A vehicle was marked as unavailable.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "SERVICE_STATUS",
            GroupName = "Vehicle Service Status",
            GroupDescription = "Events related to vehicle availability.",

            EventCode = "VEHICLE_RETURNED_TO_SERVICE",
            EventName = "Vehicle Returned To Service",
            EventDescription = "A vehicle returned to operational status.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "MAINTENANCE",
            GroupName = "Vehicle Maintenance",
            GroupDescription = "Events related to maintenance operations.",

            EventCode = "VEHICLE_MAINTENANCE_STARTED",
            EventName = "Vehicle Maintenance Started",
            EventDescription = "Vehicle maintenance process started.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "MAINTENANCE",
            GroupName = "Vehicle Maintenance",
            GroupDescription = "Events related to maintenance operations.",

            EventCode = "VEHICLE_MAINTENANCE_COMPLETED",
            EventName = "Vehicle Maintenance Completed",
            EventDescription = "Vehicle maintenance process completed.",
            Source = "VehicleService"
        },

        new()
        {
            CategoryCode = "VEHICLE",
            CategoryName = "Vehicle",
            CategoryDescription = "Events generated by vehicle assignment, status and maintenance.",

            GroupCode = "INCIDENTS",
            GroupName = "Vehicle Incidents",
            GroupDescription = "Events related to vehicle incidents.",

            EventCode = "VEHICLE_BREAKDOWN_REPORTED",
            EventName = "Vehicle Breakdown Reported",
            EventDescription = "A vehicle breakdown was reported.",
            Source = "VehicleService"
        },

        #endregion

        #region User Domain

        // =====================================================
        // User Domain
        // =====================================================

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "USER_LIFECYCLE",
            GroupName = "User Lifecycle",
            GroupDescription = "Events related to user creation and activation.",

            EventCode = "USER_CREATED",
            EventName = "User Created",
            EventDescription = "A new user was created.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "USER_LIFECYCLE",
            GroupName = "User Lifecycle",
            GroupDescription = "Events related to user creation and activation.",

            EventCode = "USER_ACTIVATED",
            EventName = "User Activated",
            EventDescription = "A user account was activated.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "USER_LIFECYCLE",
            GroupName = "User Lifecycle",
            GroupDescription = "Events related to user creation and activation.",

            EventCode = "USER_DEACTIVATED",
            EventName = "User Deactivated",
            EventDescription = "A user account was deactivated.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "SECURITY",
            GroupName = "Security",
            GroupDescription = "Events related to account security.",

            EventCode = "USER_LOCKED",
            EventName = "User Locked",
            EventDescription = "A user account was locked.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "SECURITY",
            GroupName = "Security",
            GroupDescription = "Events related to account security.",

            EventCode = "USER_UNLOCKED",
            EventName = "User Unlocked",
            EventDescription = "A user account was unlocked.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "SECURITY",
            GroupName = "Security",
            GroupDescription = "Events related to account security.",

            EventCode = "PASSWORD_CHANGED",
            EventName = "Password Changed",
            EventDescription = "A user password was changed.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "PERMISSIONS",
            GroupName = "Permissions",
            GroupDescription = "Events related to roles and permissions.",

            EventCode = "ROLE_ASSIGNED",
            EventName = "Role Assigned",
            EventDescription = "A role was assigned to a user.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "PERMISSIONS",
            GroupName = "Permissions",
            GroupDescription = "Events related to roles and permissions.",

            EventCode = "ROLE_REMOVED",
            EventName = "Role Removed",
            EventDescription = "A role was removed from a user.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "PERMISSIONS",
            GroupName = "Permissions",
            GroupDescription = "Events related to roles and permissions.",

            EventCode = "PERMISSION_GRANTED",
            EventName = "Permission Granted",
            EventDescription = "A permission was granted.",
            Source = "UserService"
        },

        new()
        {
            CategoryCode = "USER",
            CategoryName = "User",
            CategoryDescription = "Events generated by user lifecycle and security management.",

            GroupCode = "PERMISSIONS",
            GroupName = "Permissions",
            GroupDescription = "Events related to roles and permissions.",

            EventCode = "PERMISSION_REVOKED",
            EventName = "Permission Revoked",
            EventDescription = "A permission was revoked.",
            Source = "UserService"
        },

        #endregion

        #region Dispatch Domain

        // =====================================================
        // Dispatch Domain
        // =====================================================

        new()
        {
            CategoryCode = "DISPATCH",
            CategoryName = "Dispatch",
            CategoryDescription = "Events generated by dispatcher operations and trip management.",

            GroupCode = "TRIP_ASSIGNMENT",
            GroupName = "Trip Assignment",
            GroupDescription = "Events related to dispatcher trip assignment operations.",

            EventCode = "DISPATCHER_ASSIGNED_TRIP",
            EventName = "Dispatcher Assigned Trip",
            EventDescription = "Dispatcher assigned one or more trips.",
            Source = "DispatchService"
        },

        new()
        {
            CategoryCode = "DISPATCH",
            CategoryName = "Dispatch",
            CategoryDescription = "Events generated by dispatcher operations and trip management.",

            GroupCode = "TRIP_MANAGEMENT",
            GroupName = "Trip Management",
            GroupDescription = "Events related to dispatcher modifications.",

            EventCode = "DISPATCHER_MODIFIED_TRIP",
            EventName = "Dispatcher Modified Trip",
            EventDescription = "Dispatcher modified trip information.",
            Source = "DispatchService"
        },

        new()
        {
            CategoryCode = "DISPATCH",
            CategoryName = "Dispatch",
            CategoryDescription = "Events generated by dispatcher operations and trip management.",

            GroupCode = "TRIP_MANAGEMENT",
            GroupName = "Trip Management",
            GroupDescription = "Events related to dispatcher modifications.",

            EventCode = "DISPATCHER_CANCELLED_TRIP",
            EventName = "Dispatcher Cancelled Trip",
            EventDescription = "Dispatcher cancelled a trip.",
            Source = "DispatchService"
        },

        new()
        {
            CategoryCode = "DISPATCH",
            CategoryName = "Dispatch",
            CategoryDescription = "Events generated by dispatcher operations and trip management.",

            GroupCode = "TRIP_ASSIGNMENT",
            GroupName = "Trip Assignment",
            GroupDescription = "Events related to dispatcher trip assignment operations.",

            EventCode = "DISPATCHER_REASSIGNED_TRIP",
            EventName = "Dispatcher Reassigned Trip",
            EventDescription = "Dispatcher reassigned a trip to another provider or driver.",
            Source = "DispatchService"
        },

        new()
        {
            CategoryCode = "DISPATCH",
            CategoryName = "Dispatch",
            CategoryDescription = "Events generated by dispatcher operations and trip management.",

            GroupCode = "ROUTE_MANAGEMENT",
            GroupName = "Route Management",
            GroupDescription = "Events related to dispatcher route creation.",

            EventCode = "DISPATCHER_CREATED_ROUTE",
            EventName = "Dispatcher Created Route",
            EventDescription = "Dispatcher created a route.",
            Source = "DispatchService"
        },

        new()
        {
            CategoryCode = "DISPATCH",
            CategoryName = "Dispatch",
            CategoryDescription = "Events generated by dispatcher operations and trip management.",

            GroupCode = "ROUTE_MANAGEMENT",
            GroupName = "Route Management",
            GroupDescription = "Events related to dispatcher route creation.",

            EventCode = "DISPATCHER_PUBLISHED_ROUTE",
            EventName = "Dispatcher Published Route",
            EventDescription = "Dispatcher published a route.",
            Source = "DispatchService"
        },

        #endregion

        #region Communication Domain

        // =====================================================
        // Communication Domain
        // =====================================================

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "NOTIFICATION_LIFECYCLE",
            GroupName = "Notification Lifecycle",
            GroupDescription = "Events related to notification processing.",

            EventCode = "NOTIFICATION_CREATED",
            EventName = "Notification Created",
            EventDescription = "A notification was created.",
            Source = "NotificationService"
        },

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "NOTIFICATION_LIFECYCLE",
            GroupName = "Notification Lifecycle",
            GroupDescription = "Events related to notification processing.",

            EventCode = "NOTIFICATION_DELIVERED",
            EventName = "Notification Delivered",
            EventDescription = "A notification was successfully delivered to a recipient.",
            Source = "NotificationService"
        },

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "NOTIFICATION_LIFECYCLE",
            GroupName = "Notification Lifecycle",
            GroupDescription = "Events related to notification processing.",

            EventCode = "NOTIFICATION_READ",
            EventName = "Notification Read",
            EventDescription = "A recipient read a notification.",
            Source = "NotificationService"
        },

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "NOTIFICATION_LIFECYCLE",
            GroupName = "Notification Lifecycle",
            GroupDescription = "Events related to notification processing.",

            EventCode = "NOTIFICATION_ACKNOWLEDGED",
            EventName = "Notification Acknowledged",
            EventDescription = "A recipient acknowledged a notification.",
            Source = "NotificationService"
        },

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "NOTIFICATION_LIFECYCLE",
            GroupName = "Notification Lifecycle",
            GroupDescription = "Events related to notification processing.",

            EventCode = "NOTIFICATION_EXPIRED",
            EventName = "Notification Expired",
            EventDescription = "A notification expired without completion.",
            Source = "NotificationService"
        },

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "MESSAGING",
            GroupName = "Messaging",
            GroupDescription = "Events related to message exchange.",

            EventCode = "MESSAGE_SENT",
            EventName = "Message Sent",
            EventDescription = "A message was sent.",
            Source = "CommunicationService"
        },

        new()
        {
            CategoryCode = "COMMUNICATION",
            CategoryName = "Communication",
            CategoryDescription = "Events generated by communication and notification lifecycle.",

            GroupCode = "MESSAGING",
            GroupName = "Messaging",
            GroupDescription = "Events related to message exchange.",

            EventCode = "MESSAGE_RECEIVED",
            EventName = "Message Received",
            EventDescription = "A message was received.",
            Source = "CommunicationService"
        },

        #endregion

        #region ETA Domain

        // =====================================================
        // ETA Domain
        // =====================================================

        new()
        {
            CategoryCode = "ETA",
            CategoryName = "ETA",
            CategoryDescription = "Events generated by estimated time of arrival calculations and changes.",

            GroupCode = "ETA_CALCULATION",
            GroupName = "ETA Calculation",
            GroupDescription = "Events related to ETA updates and confirmations.",

            EventCode = "ETA_CHANGED",
            EventName = "ETA Changed",
            EventDescription = "The estimated arrival time changed.",
            Source = "ETAService"
        },

        new()
        {
            CategoryCode = "ETA",
            CategoryName = "ETA",
            CategoryDescription = "Events generated by estimated time of arrival calculations and changes.",

            GroupCode = "ETA_CALCULATION",
            GroupName = "ETA Calculation",
            GroupDescription = "Events related to ETA updates and confirmations.",

            EventCode = "ETA_CONFIRMED",
            EventName = "ETA Confirmed",
            EventDescription = "The estimated arrival time was confirmed.",
            Source = "ETAService"
        },

        new()
        {
            CategoryCode = "ETA",
            CategoryName = "ETA",
            CategoryDescription = "Events generated by estimated time of arrival calculations and changes.",

            GroupCode = "DELAY_DETECTION",
            GroupName = "Delay Detection",
            GroupDescription = "Events related to delayed arrival detection.",

            EventCode = "DELAYED_ARRIVAL_DETECTED",
            EventName = "Delayed Arrival Detected",
            EventDescription = "A possible delayed arrival was detected.",
            Source = "ETAService"
        },

        new()
        {
            CategoryCode = "ETA",
            CategoryName = "ETA",
            CategoryDescription = "Events generated by estimated time of arrival calculations and changes.",

            GroupCode = "EARLY_ARRIVAL",
            GroupName = "Early Arrival",
            GroupDescription = "Events related to early arrival detection.",

            EventCode = "EARLY_ARRIVAL_DETECTED",
            EventName = "Early Arrival Detected",
            EventDescription = "An early arrival was detected.",
            Source = "ETAService"
        },

        #endregion

        #region Booking Domain

        // =====================================================
        // Booking Domain
        // =====================================================

        new()
        {
            CategoryCode = "BOOKING",
            CategoryName = "Booking",
            CategoryDescription = "Events generated by external booking operations and trip requests.",

            GroupCode = "BOOKING_LIFECYCLE",
            GroupName = "Booking Lifecycle",
            GroupDescription = "Events related to booking creation, update and cancellation.",

            EventCode = "BOOKING_CREATED",
            EventName = "Booking Created",
            EventDescription = "A clinic or external customer created a booking.",
            Source = "BookingService"
        },

        new()
        {
            CategoryCode = "BOOKING",
            CategoryName = "Booking",
            CategoryDescription = "Events generated by external booking operations and trip requests.",

            GroupCode = "BOOKING_LIFECYCLE",
            GroupName = "Booking Lifecycle",
            GroupDescription = "Events related to booking creation, update and cancellation.",

            EventCode = "BOOKING_UPDATED",
            EventName = "Booking Updated",
            EventDescription = "A booking was updated by the external requester.",
            Source = "BookingService"
        },

        new()
        {
            CategoryCode = "BOOKING",
            CategoryName = "Booking",
            CategoryDescription = "Events generated by external booking operations and trip requests.",

            GroupCode = "BOOKING_LIFECYCLE",
            GroupName = "Booking Lifecycle",
            GroupDescription = "Events related to booking creation, update and cancellation.",

            EventCode = "BOOKING_CANCELLED",
            EventName = "Booking Cancelled",
            EventDescription = "A booking was cancelled by the external requester.",
            Source = "BookingService"
        },

        new()
        {
            CategoryCode = "BOOKING",
            CategoryName = "Booking",
            CategoryDescription = "Events generated by external booking operations and trip requests.",

            GroupCode = "BOOKING_APPROVAL",
            GroupName = "Booking Approval",
            GroupDescription = "Events related to booking validation and approval.",

            EventCode = "BOOKING_APPROVED",
            EventName = "Booking Approved",
            EventDescription = "A booking was approved.",
            Source = "BookingService"
        },

        new()
        {
            CategoryCode = "BOOKING",
            CategoryName = "Booking",
            CategoryDescription = "Events generated by external booking operations and trip requests.",

            GroupCode = "BOOKING_APPROVAL",
            GroupName = "Booking Approval",
            GroupDescription = "Events related to booking validation and approval.",

            EventCode = "BOOKING_REJECTED",
            EventName = "Booking Rejected",
            EventDescription = "A booking was rejected.",
            Source = "BookingService"
        },

        #endregion

        #region Integration Domain

        // =====================================================
        // Integration Domain
        // =====================================================

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "INTEGRATION_PROCESS",
            GroupName = "Integration Process",
            GroupDescription = "Events related to integration execution.",

            EventCode = "INTEGRATION_STARTED",
            EventName = "Integration Started",
            EventDescription = "An integration process started.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "INTEGRATION_PROCESS",
            GroupName = "Integration Process",
            GroupDescription = "Events related to integration execution.",

            EventCode = "INTEGRATION_COMPLETED",
            EventName = "Integration Completed",
            EventDescription = "An integration process completed successfully.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "INTEGRATION_PROCESS",
            GroupName = "Integration Process",
            GroupDescription = "Events related to integration execution.",

            EventCode = "INTEGRATION_FAILED",
            EventName = "Integration Failed",
            EventDescription = "An integration process failed.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "DATA_IMPORT_EXPORT",
            GroupName = "Data Import Export",
            GroupDescription = "Events related to external trip data exchange.",

            EventCode = "EXTERNAL_TRIP_IMPORTED",
            EventName = "External Trip Imported",
            EventDescription = "A trip was imported from an external system.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "DATA_IMPORT_EXPORT",
            GroupName = "Data Import Export",
            GroupDescription = "Events related to external trip data exchange.",

            EventCode = "EXTERNAL_TRIP_EXPORTED",
            EventName = "External Trip Exported",
            EventDescription = "A trip was exported to an external system.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "SYNCHRONIZATION",
            GroupName = "Synchronization",
            GroupDescription = "Events related to synchronization batches.",

            EventCode = "SYNCHRONIZATION_STARTED",
            EventName = "Synchronization Started",
            EventDescription = "A synchronization process started.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "SYNCHRONIZATION",
            GroupName = "Synchronization",
            GroupDescription = "Events related to synchronization batches.",

            EventCode = "SYNCHRONIZATION_COMPLETED",
            EventName = "Synchronization Completed",
            EventDescription = "A synchronization process completed successfully.",
            Source = "IntegrationService"
        },

        new()
        {
            CategoryCode = "INTEGRATION",
            CategoryName = "Integration",
            CategoryDescription = "Events generated by external system integrations and synchronization processes.",

            GroupCode = "SYNCHRONIZATION",
            GroupName = "Synchronization",
            GroupDescription = "Events related to synchronization batches.",

            EventCode = "SYNCHRONIZATION_FAILED",
            EventName = "Synchronization Failed",
            EventDescription = "A synchronization process failed.",
            Source = "IntegrationService"
        },

        #endregion

        #region System Domain

        // =====================================================
        // System Domain
        // =====================================================

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "SYSTEM_LIFECYCLE",
            GroupName = "System Lifecycle",
            GroupDescription = "Events related to system availability and lifecycle.",

            EventCode = "SYSTEM_STARTED",
            EventName = "System Started",
            EventDescription = "A system service or component started.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "SYSTEM_LIFECYCLE",
            GroupName = "System Lifecycle",
            GroupDescription = "Events related to system availability and lifecycle.",

            EventCode = "SYSTEM_STOPPED",
            EventName = "System Stopped",
            EventDescription = "A system service or component stopped.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "CONFIGURATION",
            GroupName = "Configuration",
            GroupDescription = "Events related to system configuration changes.",

            EventCode = "CONFIGURATION_CHANGED",
            EventName = "Configuration Changed",
            EventDescription = "System configuration was modified.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "DATABASE",
            GroupName = "Database",
            GroupDescription = "Events related to database maintenance.",

            EventCode = "DATABASE_BACKUP_COMPLETED",
            EventName = "Database Backup Completed",
            EventDescription = "A database backup completed successfully.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "SERVICE_AVAILABILITY",
            GroupName = "Service Availability",
            GroupDescription = "Events related to service availability.",

            EventCode = "SERVICE_UNAVAILABLE",
            EventName = "Service Unavailable",
            EventDescription = "A system service became unavailable.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "SERVICE_AVAILABILITY",
            GroupName = "Service Availability",
            GroupDescription = "Events related to service availability.",

            EventCode = "SERVICE_RECOVERED",
            EventName = "Service Recovered",
            EventDescription = "A system service recovered after an outage.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "LICENSE",
            GroupName = "License Management",
            GroupDescription = "Events related to license lifecycle.",

            EventCode = "LICENSE_EXPIRED",
            EventName = "License Expired",
            EventDescription = "A system license expired.",
            Source = "System"
        },

        new()
        {
            CategoryCode = "SYSTEM",
            CategoryName = "System",
            CategoryDescription = "Events generated by system lifecycle and infrastructure operations.",

            GroupCode = "LICENSE",
            GroupName = "License Management",
            GroupDescription = "Events related to license lifecycle.",

            EventCode = "LICENSE_RENEWED",
            EventName = "License Renewed",
            EventDescription = "A system license was renewed.",
            Source = "System"
        },

        #endregion

    ];
}