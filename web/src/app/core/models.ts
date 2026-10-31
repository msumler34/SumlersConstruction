export interface ServiceOffering {
  key: string;
  title: string;
  summary: string;
  icon: string;
  imageUrl: string;
  highlights: string[];
}

export interface Testimonial {
  quote: string;
  author: string;
  project: string;
}

export interface GalleryImage {
  imageUrl: string;
  label: string;
  alt: string;
}

export interface HeroText {
  eyebrow: string;
  title: string;
  highlight: string;
  text: string;
}

export interface SectionItem {
  icon: string;
  title: string;
  text: string;
}

/** A home page section: heading, intro text, optional photo and a list of points. */
export interface PageSection {
  title: string;
  text: string;
  imageUrl: string;
  items: SectionItem[];
}

export interface SeoText {
  title: string;
  description: string;
}

export interface SiteInfo {
  businessName: string;
  tagline: string;
  phone: string;
  email: string;
  address: string;
  serviceArea: string;
  hours: string;
  facebookUrl: string;
  messengerUrl: string;
  yearsInBusiness: number;
  heroImageUrl: string;
  services: ServiceOffering[];
  testimonials: Testimonial[];
  gallery: GalleryImage[];
  hero: HeroText;
  whyUs: PageSection;
  process: PageSection;
  seo: SeoText;
}

export interface EmailSettings {
  host: string;
  port: number;
  useStartTls: boolean;
  username: string;
  password: string;
  fromAddress: string;
  fromName: string;
  notifyAddresses: string[];
  sendCustomerConfirmation: boolean;
}

export interface AlertSettings {
  recipients: string[];
  emailWeeklyBackup: boolean;
}

/** Admin settings as the server returns them: the password is always blank, hasPassword says if one is saved. */
export interface SettingsView {
  email: EmailSettings;
  alerts: AlertSettings;
  hasPassword: boolean;
}

export interface ContactMessage {
  id: string;
  receivedAt: string;
  name: string;
  email: string;
  phone: string;
  service: string;
  address: string;
  message: string;
  emailStatus: 'Pending' | 'Sent' | 'Failed';
  isRead: boolean;
}

export interface BackupInfo {
  name: string;
  createdAt: string;
  sizeBytes: number;
  reason: string;
}

export interface Promotion {
  id: string;
  title: string;
  summary: string;
  badge: string | null;
  imageUrl: string | null;
  ctaText: string | null;
  ctaUrl: string | null;
  isFeatured: boolean;
  isActive: boolean;
  sortOrder: number;
  startsOn: string | null;
  endsOn: string | null;
}

export interface ContactRequest {
  name: string;
  email: string;
  phone: string;
  service: string;
  address: string;
  message: string;
  website: string;
}
