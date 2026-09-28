export const fileColumns = [
  { key: "fileKey", label: "File" },
  { key: "exists", label: "Status", format: (value) => (value ? "Available" : "Missing") },
];

export const eventColumns = [
  { key: "eventType", label: "Event" },
  { key: "detail", label: "Detail" },
  {
    key: "occurredAt",
    label: "Occurred",
    format: formatDate,
  },
];

function formatDate(date) {
  return new Date(date).toLocaleString();
}

export const nodeColumns = [
  { key: "id", label: "Node" },
  { key: "url", label: "URL" },
];
