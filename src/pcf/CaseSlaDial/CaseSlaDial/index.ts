import { IInputs, IOutputs } from "./generated/ManifestTypes";

/**
 * CaseSlaDial - renders the server-computed RAG band as a dial.
 *
 * This component contains NO banding logic. The four band names below are the
 * four strings the RAG status formula column returns; this map turns each one
 * into a colour class and a human sentence. If the formula changes, this map
 * changes with it - but the threshold that decides the band never lives here.
 */

interface BandPresentation {
    cssSuffix: string;
    label: string;
}

const BANDS: Record<string, BandPresentation> = {
    "Red":     { cssSuffix: "red",    label: "Overdue" },
    "Amber":   { cssSuffix: "amber",  label: "Due soon" },
    "Green":   { cssSuffix: "green",  label: "On track" },
    "Not set": { cssSuffix: "notset", label: "No SLA set" }
};

const UNKNOWN: BandPresentation = { cssSuffix: "notset", label: "Unknown" };

const RADIUS = 26;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;
const MINIMUM_VISIBLE_ARC = 0.03;

export class CaseSlaDial
    implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private _container: HTMLDivElement;

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this._container = container;
        this._container.classList.add("helix-dial");
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        const bandValue: string | null = context.parameters.ragStatus.raw;
        const hours: number | null = context.parameters.hoursRemaining.raw;
        const horizon: number = this.horizon(context);

        const presentation: BandPresentation =
            bandValue !== null && BANDS[bandValue] !== undefined
                ? BANDS[bandValue]
                : UNKNOWN;

        this.render(presentation, this.arcFraction(hours, horizon), this.detail(hours));
    }

    /** How full the arc is drawn. Presentation only - decides no band. */
    private arcFraction(hours: number | null, horizon: number): number {
        if (hours === null || hours <= 0) {
            return 0;
        }
        return Math.min(hours / horizon, 1);
    }

    private horizon(context: ComponentFramework.Context<IInputs>): number {
        const raw = context.parameters.horizonHours.raw;
        return raw === null || raw <= 0 ? 72 : raw;
    }

    /** The second line of text. Says nothing about urgency. */
    private detail(hours: number | null): string {
        if (hours === null) {
            return "No deadline recorded";
        }
        const whole = Math.round(Math.abs(hours));
        const unit = whole === 1 ? "hour" : "hours";
        return hours <= 0
            ? whole + " " + unit + " past the deadline"
            : whole + " " + unit + " remaining";
    }

    private render(band: BandPresentation, fraction: number, detail: string): void {
        const dash = Math.max(fraction, MINIMUM_VISIBLE_ARC) * CIRCUMFERENCE;
        const gap = CIRCUMFERENCE - dash;
        const describedBy = band.label + ". " + detail + ".";

        this._container.innerHTML =
            '<svg width="64" height="64" viewBox="0 0 64 64" role="img" aria-label="' +
                this.escape(describedBy) + '">' +
                '<circle class="helix-track" cx="32" cy="32" r="' + RADIUS +
                    '" fill="none" stroke-width="7" />' +
                '<circle class="helix-arc-' + band.cssSuffix + '" cx="32" cy="32" r="' + RADIUS +
                    '" fill="none" stroke-width="7" stroke-linecap="round" ' +
                    'stroke-dasharray="' + dash.toFixed(2) + " " + gap.toFixed(2) + '" ' +
                    'transform="rotate(-90 32 32)" />' +
            "</svg>" +
            '<span class="helix-dial-text">' +
                '<span class="helix-dial-band">' + this.escape(band.label) + "</span>" +
                '<span class="helix-dial-detail">' + this.escape(detail) + "</span>" +
            "</span>";
    }

    private escape(value: string): string {
        return value
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    public getOutputs(): IOutputs {
        return {};
    }

    public destroy(): void {
        this._container.innerHTML = "";
    }
}