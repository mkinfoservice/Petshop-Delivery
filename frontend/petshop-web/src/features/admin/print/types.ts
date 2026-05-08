export type PrintItemPayload = {
  name: string;
  qty: number;
  unitCents: number;
};

export type PrintBrandingPayload = {
  storeName: string;
  storeSlogan: string | null;
  logoUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
};

export type PrintOrderPayload = {
  orderId: string;
  publicId: string;
  customerName: string;
  phone: string;
  address: string;
  complement: string | null;
  cep: string;
  paymentMethod: string;
  totalCents: number;
  subtotalCents: number;
  deliveryCents: number;
  cashGivenCents: number | null;
  changeCents: number | null;
  isPhoneOrder: boolean;
  createdAtUtc: string;
  branding?: PrintBrandingPayload | null;
  items: PrintItemPayload[];
};

export type PrintJob = {
  jobId: string;
  payload: PrintOrderPayload;
};

export type PendingJobDto = {
  id: string;
  publicId: string;
  printPayloadJson: string;
  createdAtUtc: string;
};

export type PrintJobDto = {
  id: string;
  orderId: string;
  publicId: string;
  isPrinted: boolean;
  createdAtUtc: string;
  printedAtUtc: string | null;
};
