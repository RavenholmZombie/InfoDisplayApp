# CefSharp browser experiment

This branch contains a standalone proof-of-concept for using CefSharp/Chromium as the browser renderer while keeping InfoDisplayApp's bottom information bar permanently visible.

## Running the experiment

Run InfoDisplayApp with the existing experimental switch:

```text
--firefox-kiosk-experiment
```

The switch name is intentionally preserved so the current Visual Studio debug profile does not need to change. With no argument, InfoDisplayApp still launches the normal `frmMain`.

## Layout

The test form intentionally mirrors the important part of `frmMain`:

- a large browser/TV area fills the upper portion of the display;
- a persistent 100-pixel information/control bar occupies the bottom;
- CefSharp's `ChromiumWebBrowser` is docked directly into the browser area;
- no external browser window, z-order watchdog, `SetWindowPos`, or `SetParent` logic is required.

Because the browser is a normal WinForms control, its actual viewport ends where the InfoDisplay bottom bar begins.

## Browser data

The experiment initializes CefSharp with a persistent cache at:

```text
%LOCALAPPDATA%\InfoDisplayApp\CefSharpExperiment
```

This lets the experiment retain browser state between launches while remaining isolated from the normal WebView2 controls and user browser profiles.

## Test buttons

- **Philo** navigates to `https://www.philo.com/player/mytv`.
- **YouTube** navigates to YouTube.
- **Example** navigates to a simple test page.
- **Stop** replaces the current page with `about:blank`.
- **Close Test** exits the experiment and shuts down CefSharp cleanly.

## Things to verify

1. CefSharp fills the upper viewport exactly and never overlaps the bottom bar.
2. Philo loads and allows normal sign-in/navigation.
3. Live channels can actually play, including any required DRM/media pipeline.
4. Audio works normally.
5. Video controls reflow inside the shortened browser viewport.
6. Long-running Philo playback does not develop the A/V drift previously seen in WebView2.
7. YouTube playback behaves normally as a secondary media test.

This is intentionally isolated experimental code; it does not replace the existing WebView2 Philo/YouTube controls on `frmMain`.
