import { getTheme, IButtonStyles, IDropdownOption, IDropdownStyles, IIconProps, ISearchBoxStyles, IStackItemStyles, IStackStyles, IStackTokens } from "@fluentui/react";

const theme = getTheme();
export const iconButtonStyles: Partial<IButtonStyles> = {
    root: {
        color: theme.palette.neutralPrimary,
        marginLeft: 'auto',

        marginRight: '2px',
    },
    rootHovered: {
        color: theme.palette.neutralDark,
    },
};
export const approveAll: Partial<IButtonStyles> = {
    root: {
        color: "blue",
        boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
        borderColor: "white",
        width: "100%"
    }
}
export const refreshButton: Partial<IButtonStyles> = {
    root: {
        padding: 5,
        width: "100%",
    }
}

export const exportsty: Partial<IButtonStyles> = {
    root: {
        color: "white",
        borderColor: "blue",
        width: "100%",
        boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    }
}

export const uidCreate: Partial<IButtonStyles> = {
    root: {
        color: "blue",
        marginLeft: "70%",
        fontWeight: "bold",
        boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
        border: "white",
    }
}

export const MessageBarStyle: Partial<any> = {
    root: {
        position: "absolute",
        zIndex: 9999,
        boxShadow: "0 0 3px 1px #afafaf",
        width: "auto",
        alignContent: "center",
        left: "50%"
    }
}
export const assignBtn: Partial<IButtonStyles> = {
    root: {
        width: "100%"

    }
}
export const assignRoleBtn: Partial<IButtonStyles> = {
    root: {
        width: "100%"


    }
}
