// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React from 'react';
import {
    MenuItem,
    Tooltip,
    IconButton,
    Menu,
    ListItemIcon,
    Avatar,
} from '@mui/material';
import Fade from '@mui/material/Fade';
import LogoutIcon from '@mui/icons-material/Logout';
import { useAuth } from 'react-oidc-context';
import { signOut } from '../auth/authSession';

export default function LogoutMenu() {
    const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null)
    
    const open = Boolean(anchorEl);
    const auth = useAuth()

    const handleClose = () => {
        setAnchorEl(null)
    }

    const handleClick = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };


    return (
        <div>
            <IconButton onClick={handleClick}>
                <Avatar>
                    {auth.user?.profile.name?.charAt(0) ?? ''}
                </Avatar>
            </IconButton>

            <Menu
                anchorEl={anchorEl}
                open={open}
                onClose={handleClose}
                TransitionComponent={Fade}>
                <div className="flex flex-wrap gap-2 max-w-75 justify-around">
                    <Tooltip title={'Sign out'} arrow>
                        <MenuItem
                            onClick={() => {
                                // Shared sign-out: clears local state first and
                                // falls back to a local reset if Keycloak is
                                // unreachable.
                                void signOut()
                            }}
                            className="box-border flex w-[95%] justify-center items-center text-center"
                        >
                            Abmelden
                            <ListItemIcon className="pl-2">
                                <LogoutIcon />
                            </ListItemIcon>
                        </MenuItem>
                    </Tooltip>
                </div>
            </Menu>
        </div>
    );
}
