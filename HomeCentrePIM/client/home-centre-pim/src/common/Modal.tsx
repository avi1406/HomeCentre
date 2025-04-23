import { FontWeights, getTheme, mergeStyleSets } from "@fluentui/react";


const theme = getTheme()
export const contentStyles = mergeStyleSets({
    container: {
        display: 'flex',
        flexFlow: 'column nowrap',
        alignItems: 'stretch',
    },
    header: [
        // eslint-disable-next-line deprecation/deprecation
        theme.fonts.xLargePlus,
        {
            flex: '1 1 auto',
            color: theme.palette.neutralPrimary,
            display: 'flex',
            alignItems: 'center',
            fontWeight: FontWeights.semibold,
            padding: '12px 12px 14px 24px',
            height: '100px'
        },
    ],
    body: {
        flex: '4 4 auto',
        overflowY: 'hidden',
        marginTop: "-30px",
        display: 'block',
        alignItems: 'strech',
        width: '300px',
        height: "70%",
        marginLeft: "10px",
        marginRight: "10px"
    },
    assign_body: {
        width: '400px'
    },
    footer: [
        // eslint-disable-next-line deprecation/deprecation
        theme.fonts.xLargePlus,
        {
            flex: '1 1 auto',
            color: theme.palette.neutralPrimary,
            display: 'flex',
            alignItems: 'center',
            fontWeight: FontWeights.semibold,
            padding: '12px 12px 14px 24px',
        },
    ],
});