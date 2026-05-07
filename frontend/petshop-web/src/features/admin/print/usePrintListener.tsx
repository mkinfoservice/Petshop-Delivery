import { useCallback, useEffect, useRef, useState } from "react";
import { createRoot } from "react-dom/client";
import type { HubConnection } from "@microsoft/signalr";
import { getToken, decodeTokenPayload } from "@/features/admin/auth/auth";
import { fetchPendingPrintJobs, markPrinted } from "./api";
import type { PrintOrderPayload, PendingJobDto } from "./types";
import { PrintReceipt } from "./PrintReceipt";
import {
  isMobileAgent,
  mobilePrint,
  registerBrowserPrintFn,
} from "./mobilePrint";

export type { PrintOrderPayload };

const API_URL = import.meta.env.VITE_API_URL ?? "";

export const PRINT_STATION_KEY = "vendapps_print_station";

export function isPrintStation(): boolean {
  return localStorage.getItem(PRINT_STATION_KEY) === "1";
}

export function setPrintStation(on: boolean) {
  if (on) localStorage.setItem(PRINT_STATION_KEY, "1");
  else localStorage.removeItem(PRINT_STATION_KEY);
}

function printViaBrowser(payload: PrintOrderPayload, jobId: string) {
  const wrapper = document.createElement("div");
  wrapper.className = "receipt-print-wrapper";
  document.body.appendChild(wrapper);

  const root = createRoot(wrapper);
  root.render(<PrintReceipt payload={payload} />);

  requestAnimationFrame(() => {
    wrapper.style.display = "block";
    window.print();

    setTimeout(() => {
      root.unmount();
      wrapper.remove();
      markPrinted(jobId).catch(() => {/* best effort */});
    }, 500);
  });
}

registerBrowserPrintFn(printViaBrowser);

function printPayload(payload: PrintOrderPayload, jobId: string) {
  if (isMobileAgent()) {
    mobilePrint(payload, jobId).catch((err) =>
      console.error("[MobileAgent] Erro na impressao:", err),
    );
    return;
  }

  printViaBrowser(payload, jobId);
}

export function usePrintListener(onNewOrder?: (payload: PrintOrderPayload) => void) {
  const connectionRef = useRef<HubConnection | null>(null);
  const onNewOrderRef = useRef(onNewOrder);
  const [connected, setConnected] = useState(false);
  const [printStation, setPrintStationState] = useState<boolean>(isPrintStation);

  useEffect(() => {
    onNewOrderRef.current = onNewOrder;
  }, [onNewOrder]);

  function togglePrintStation() {
    const next = !printStation;
    setPrintStation(next);
    setPrintStationState(next);
  }

  const replayPending = useCallback(async () => {
    if (!isPrintStation()) return;
    try {
      const jobs: PendingJobDto[] = await fetchPendingPrintJobs();
      for (const job of jobs) {
        try {
          const payload: PrintOrderPayload = JSON.parse(job.printPayloadJson);
          printPayload(payload, job.id);
          await new Promise((resolve) => setTimeout(resolve, 1500));
        } catch {
          // Ignore corrupt jobs.
        }
      }
    } catch {
      // Ignore transient network failures.
    }
  }, []);

  useEffect(() => {
    const token = getToken();
    if (!token) return;

    const decoded = decodeTokenPayload(token);
    const companyId = decoded?.companyId;
    if (!companyId) return;

    let cancelled = false;
    let connection: HubConnection | null = null;

    void import("@microsoft/signalr").then(async (signalR) => {
      if (cancelled) return;

      connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_URL}/hubs/print?access_token=${token}`, {
          transport: signalR.HttpTransportType.WebSockets |
                     signalR.HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .withKeepAliveInterval(10_000)
        .withServerTimeout(60_000)
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      connection.on("PrintOrder", (data: { jobId: string; payload: PrintOrderPayload }) => {
        if (isPrintStation()) {
          printPayload(data.payload, data.jobId);
        }
        onNewOrderRef.current?.(data.payload);
      });

      connection.onreconnected(async () => {
        setConnected(true);
        await connection?.invoke("JoinCompany", companyId);
        await replayPending();
      });

      connection.onclose(() => setConnected(false));
      connectionRef.current = connection;

      try {
        await connection.start();
        if (cancelled) {
          await connection.stop();
          return;
        }
        await connection.invoke("JoinCompany", companyId);
        setConnected(true);
        await replayPending();
      } catch {
        // Automatic reconnect is configured after the first successful start.
      }
    });

    return () => {
      cancelled = true;
      void (connection ?? connectionRef.current)?.stop();
      connectionRef.current = null;
      setConnected(false);
    };
  }, [replayPending]);

  return { connected, printStation, togglePrintStation };
}
