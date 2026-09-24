### About WorldClockViewer

A simple WebGL app that displays current UTC and local date and time.

<img src=".github/main-screen.jpg" alt="Main screen of WebClockViewer" width="100%">

### Features

1. Display both analogue and digital date/time.
2. Display both UTC and local date/time.
3. Using REST API to read server time.
4. Fallback servers to handle cases when current time API fails to return ticks.
5. Setting both analogue and digital date/time - both via UI and keyboard for convenience.
6. Pinging server every one in a while to correct local time.
7. Built on Unity 2022.3.62f3 with security patches applied for WebGL build.

You can edit time by clicking on one of the analogue clocks and selecting the time that you want to set. This will override both UTC and Local time so next time you open the app it will be set the way you set it (unless you reset the custom setting):

<img src=".github/edit-analogue-time.jpg" alt="Edit analogue time" width="100%">

You can also set time by typing it via keyboard (the same principle applies of how time is being set, saved and reset as with analogue time setter):

<img src=".github/edit-digital-time.jpg" alt="Edit digital time" width="100%">