import { useState } from 'react';
import {
  AppBar,
  Avatar,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  Assignment as AssignmentIcon,
  Build as BuildIcon,
  BugReport as BugReportIcon,
  Dashboard as DashboardIcon,
  Factory as FactoryIcon,
  History as HistoryIcon,
  Inventory2 as InventoryIcon,
  Logout as LogoutIcon,
  Menu as MenuIcon,
  People as PeopleIcon,
  Settings as SettingsIcon,
  Assessment as AssessmentIcon,
} from '@mui/icons-material';
import { Link as RouterLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import type { UserRole } from '../../types/api';

const DRAWER_WIDTH = 260;

interface NavItem {
  label: string;
  path: string;
  icon: typeof DashboardIcon;
  /** When set, the item is hidden from roles not listed. */
  roles?: UserRole[];
}

const navItems: NavItem[] = [
  { label: 'Дашборд', path: '/', icon: DashboardIcon },
  { label: 'Изделия', path: '/products', icon: InventoryIcon },
  { label: 'Партии', path: '/batches', icon: FactoryIcon },
  { label: 'Проверки', path: '/inspections', icon: AssignmentIcon },
  { label: 'Дефекты', path: '/defects', icon: BugReportIcon },
  { label: 'Корректирующие действия', path: '/corrective-actions', icon: BuildIcon },
  { label: 'Отчёты', path: '/reports', icon: AssessmentIcon },
  {
    label: 'Журнал действий',
    path: '/audit',
    icon: HistoryIcon,
    roles: ['Administrator', 'ProductionManager'],
  },
  { label: 'Пользователи', path: '/users', icon: PeopleIcon, roles: ['Administrator'] },
  { label: 'Настройки', path: '/settings', icon: SettingsIcon },
];

export function AppLayout() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [userMenuAnchor, setUserMenuAnchor] = useState<null | HTMLElement>(null);
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  // Menu entries are filtered by role for tidiness. The server enforces the
  // actual permissions; hiding a link is not what stops anyone.
  const visibleItems = navItems.filter(
    (item) => !item.roles || (user && item.roles.includes(user.role)),
  );

  function isActive(path: string): boolean {
    return path === '/' ? location.pathname === '/' : location.pathname.startsWith(path);
  }

  async function handleLogout() {
    setUserMenuAnchor(null);
    await logout();
    navigate('/login', { replace: true });
  }

  const drawerContent = (
    <>
      <Toolbar sx={{ px: 2 }}>
        <Box>
          <Typography variant="h6" noWrap sx={{ lineHeight: 1.2 }}>
            АСКК
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap>
            Амкодор-Пинск
          </Typography>
        </Box>
      </Toolbar>

      <Divider />

      <List sx={{ px: 1, py: 1 }}>
        {visibleItems.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.path);

          return (
            <ListItemButton
              key={item.path}
              component={RouterLink}
              to={item.path}
              selected={active}
              onClick={() => !isDesktop && setMobileOpen(false)}
              sx={{ borderRadius: 1, mb: 0.5 }}
            >
              <ListItemIcon sx={{ minWidth: 40, color: active ? 'primary.main' : undefined }}>
                <Icon fontSize="small" />
              </ListItemIcon>
              <ListItemText
                primary={item.label}
                primaryTypographyProps={{ fontSize: 14, fontWeight: active ? 600 : 400 }}
              />
            </ListItemButton>
          );
        })}
      </List>
    </>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
          borderBottom: '1px solid',
          borderColor: 'divider',
        }}
      >
        <Toolbar>
          <IconButton
            edge="start"
            onClick={() => setMobileOpen(true)}
            sx={{ mr: 2, display: { md: 'none' } }}
            aria-label="Открыть меню"
          >
            <MenuIcon />
          </IconButton>

          <Typography variant="h6" sx={{ flexGrow: 1, fontSize: 16 }}>
            {visibleItems.find((i) => isActive(i.path))?.label ?? ''}
          </Typography>

          <Tooltip title={user?.fullName ?? user?.userName ?? ''}>
            <IconButton onClick={(e) => setUserMenuAnchor(e.currentTarget)} aria-label="Меню пользователя">
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 14 }}>
                {(user?.fullName ?? user?.userName ?? '?').charAt(0).toUpperCase()}
              </Avatar>
            </IconButton>
          </Tooltip>

          <Menu
            anchorEl={userMenuAnchor}
            open={userMenuAnchor !== null}
            onClose={() => setUserMenuAnchor(null)}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          >
            <Box sx={{ px: 2, py: 1, minWidth: 220 }}>
              <Typography variant="subtitle2">{user?.fullName ?? user?.userName}</Typography>
              <Typography variant="caption" color="text.secondary" display="block">
                {user?.roleDisplayName}
              </Typography>
              {user?.departmentName ? (
                <Typography variant="caption" color="text.secondary" display="block">
                  {user.departmentName}
                </Typography>
              ) : null}
            </Box>

            <Divider />

            <MenuItem component={RouterLink} to="/settings" onClick={() => setUserMenuAnchor(null)}>
              <ListItemIcon>
                <SettingsIcon fontSize="small" />
              </ListItemIcon>
              Настройки
            </MenuItem>

            <MenuItem onClick={() => void handleLogout()}>
              <ListItemIcon>
                <LogoutIcon fontSize="small" />
              </ListItemIcon>
              Выйти
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}>
        {/* Temporary on mobile, permanent on desktop. keepMounted keeps the mobile
            drawer's markup around so opening it is instant on a slow tablet. */}
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
          }}
        >
          {drawerContent}
        </Drawer>

        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
          }}
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: { xs: 2, md: 3 },
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          minWidth: 0,
        }}
      >
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}
