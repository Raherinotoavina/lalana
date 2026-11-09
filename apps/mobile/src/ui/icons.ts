/**
 * The app's icon set (Lucide, stroke icons matching the board). Import icons from here
 * so the set stays consistent; never use emoji as icons.
 */
export {
  ChevronRight,
  CircleAlert,
  Eye,
  EyeOff,
  House,
  MessageCircle,
  Plus,
  Ticket,
  User,
} from 'lucide-react-native';

export type { LucideIcon as Icon } from 'lucide-react-native';

/** Board icons use a 1.8 stroke at 22 px. */
export const iconDefaults = { size: 22, strokeWidth: 1.8 } as const;
