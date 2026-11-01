/** A heading with text. Blank lines in the text start new paragraphs. */
export interface TextBlock {
  title: string;
  text: string;
}

export interface Faq {
  question: string;
  answer: string;
}

/** The content of a service's own page at /{key}. */
export interface ServicePage {
  heading: string;
  intro: string;
  sections: TextBlock[];
  gallery: GalleryImage[];
  faqs: Faq[];
  seoTitle: string;
  seoDescription: string;
}

export interface ServiceOffering {
  /** The page address: the service's page is at /{key}. */
  key: string;
  title: string;
  summary: string;
  icon: string;
  imageUrl: string;
  highlights: string[];
  /** Earlier page addresses, which redirect to the current one. */
  previousKeys: string[];
  page: ServicePage;
}

/** Text on the /services overview page. */
export interface ServicesPageText {
  eyebrow: string;
  title: string;
  text: string;
  imageUrl: string;
  moreTitle: string;
  moreText: string;
  moreItems: string[];
  seoTitle: string;
  seoDescription: string;
}

/** What a page tells browsers and search engines, from /api/page-meta. */
export interface PageMeta {
  statusCode: number;
  title: string;
  description: string;
  canonicalUrl: string;
  imageUrl: string;
  noIndex: boolean;
  redirectTo: string | null;
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
  /** The main address, such as https://sumlersconstruction.com. */
  siteUrl: string;
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
  servicesPage: ServicesPageText;
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
