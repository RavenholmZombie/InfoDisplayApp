# Firefox kiosk experiment

This branch contains a standalone proof-of-concept for using desktop Firefox as the browser renderer while keeping InfoDisplayApp's bottom information bar permanently visible.

## Running the experiment

Run InfoDisplayApp with:

```text
--firefox-kiosk-experiment
```

For example from Visual Studio, add that value to the project's command-line arguments for the debug profile. With no argument, InfoDisplayApp still launches the normal `frmMain`.

## Layout

The test form intentionally mirrors the important part of `frmMain`:

- a large black browser/TV area fills the upper portion of the display;
- a persistent 100-pixel information/control bar occupies the bottom;
- Firefox is launched as a separate kiosk-mode window;
- InfoDisplayApp discovers the newly-created Firefox HWND and uses `SetWindowPos` every 250 ms to constrain it to the exact screen rectangle of the browser area.

The goal is for Firefox's *real page viewport* to end above the bottom bar. Nothing should merely be hidden behind InfoDisplayApp.

## Browser profile

The experiment uses a dedicated persistent Firefox profile at:

```text
%LOCALAPPDATA%\InfoDisplayApp\FirefoxKioskProfile
```

This is intentional so Philo and YouTube can keep cookies/login/DRM state without altering the user's ordinary Firefox profile.

## Test buttons

- **Philo** launches `https://www.philo.com/player/mytv`.
- **YouTube** launches YouTube.
- **Example** launches a simple page that makes viewport sizing easy to inspect.
- **Stop** terminates only the Firefox process whose window was attached by the experiment.
- **Close Test** exits the experiment and stops its Firefox instance.

## Things to verify

1. Firefox kiosk chrome is absent.
2. The Firefox window remains constrained to the black browser area rather than snapping back to full-monitor fullscreen.
3. Page content and video controls reflow inside the shortened browser viewport and are not covered by the bottom bar.
4. Philo login and DRM playback work with the dedicated profile.
5. Long-running Philo playback does not develop the A/V drift previously seen in WebView2.
6. Clicking the InfoDisplay bottom bar remains possible while Firefox is running.

This is intentionally isolated experimental code; it does not replace the existing WebView2 Philo/YouTube controls on `frmMain`.
