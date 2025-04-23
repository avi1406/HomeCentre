import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';

class Overview extends React.Component<any, any> {
  render() {
    return (
      <div className="layout width-100 bckimg">
        <div className="bg-Color1">
          <h1 className="color-blue text wlcmlblcolor">
            <span className="label-red">{`${this.props.t('WELCOME_MSG')}`}</span>
            <br />
            <span className="label-gray">{`${this.props.t('PRODUCT_INFO_MSG')}`}</span>
          </h1>
        </div>
      </div>
    );
  }
}
const ComponentTranslated = withTranslation()(Overview);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
