import { Stack } from "@fluentui/react";
import React, { Suspense } from "react";
import { Text } from '@fluentui/react/lib/Text';
import { withTranslation } from "react-i18next";
import ApiManager from "@/services/ApiManager";
import { EmailContentModel } from "@/services/ApiClient";
import AuthManagerService from "@/services/AuthManagerService";
import Util from "@/services/Util";

class Notificationss extends React.Component<any, any> {
    apiClient = new ApiManager().CreateApiClient();
    authManager = new AuthManagerService();
    util = new Util();

    constructor(props: any) {
        super(props);
        this.state = {
            emailContentData: new EmailContentModel(),
        }
    }

    componentDidMount(): void {
        this.getMailContent().then(r => r);
    }

    async getMailContent() {
        this.apiClient.emailContent().then(r => {
            this.setState({ emailContentData: r?.result });
        });
    }

    render() {
        const { t } = this.props;
        return (
            <div className="layout width-100">
                <div className="bg-Color">
                    <Stack>
                        <Text block className="text">{`${t('PIM')} / ${t('MSG-NOTIFY-TITLE')}`}</Text>
                        <Text variant='xxLarge' className="color-blue text">{`${t('MSG-NOTIFY-TITLE')}`}</Text>
                        <div>
                            <br />
                            <hr />
                            <b>{this.authManager.getUserData()?.name}&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                {this.util.getFullDate(this.state?.emailContentData?.createdDate)}</b>
                            <hr />
                            <br /><br />
                            <b>{t('MSG-NOTIFY-EMAIL-DESCRIPTION')}</b>
                            <div style={{ marginLeft: '6vw', width: '70vw', borderStyle: 'solid', borderWidth: '2px', borderColor: 'lightgrey' }}>
                                <p style={{ marginLeft: '2vw', lineHeight: '40px', wordBreak: 'break-word' }} dangerouslySetInnerHTML={{ __html: this.state.emailContentData?.emailcontent?.replaceAll('{0}', this.authManager.getUserData()?.name)?.replaceAll('{1}', this.util.getFullDate(new Date())) || t('MSG-NOTIFY-NO-EMAIL-DESCRIPTION-CONTENT') }}></p>
                            </div>
                        </div>
                    </Stack >
                </div >
            </div>
        )
    }
}

const ComponentTranslated = withTranslation()(Notificationss);

function App() {
    return (
        <Suspense fallback="">
            <ComponentTranslated />
        </Suspense>
    );
}

export default App;