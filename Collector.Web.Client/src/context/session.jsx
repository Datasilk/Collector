import React, {useState} from 'react';
import {getUser, userContext} from './session/user';

// The Context 
const SessionContext = React.createContext({});

// Session Provider
const SessionProvider = ({children}) => {
    // context states
    const [user, setUser] = useState(getUser());
    const [modal, setModal] = useState(null);

    // context properties passed to consumer
    const context = {
        ...userContext(user, setUser),
        modal,
        setModal
    };

    //actions
    const handleLogOut = () => {
        context.setUser(null);
    }

    const showModal = (component, onComplete, onClose) => {
        context.setModal({component, onComplete, onClose});
    };

    const hideModal = () => {
        context.setModal(null);
    }

    context.logout = handleLogOut;
    context.showModal = showModal;
    context.hideModal = hideModal;

    const Modal = modal?.component ?? null;

    return (
        <SessionContext.Provider value={context}>
            <>
            {modal && <Modal onComplete={modal.onComplete} onClose={modal.onClose} />}
            {children}
            </>
        </SessionContext.Provider>
    )
};

// useSession Hook
const useSession = () => {
    const context = React.useContext(SessionContext);
    if(context === undefined) throw new Error('useSession must be used within SessionProvider');
    return context;
};

export {
    SessionProvider,
    useSession
}