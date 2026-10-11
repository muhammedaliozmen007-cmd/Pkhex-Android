"use strict";

// Uses a streamed Blazor interop reference: no save-file bytes are sent to a server.
window.pkhexDownload = async (name, streamReference) => {
  const buffer = await streamReference.arrayBuffer();
  const blob = new Blob([buffer], { type: "application/octet-stream" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = (name || "pkhex-export.bin").replace(/[\\/:*?"<>|]/g, "_");
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60000);
};
