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

    const showModal = (componentOrFunction) => {
        // If it's a function, store it as-is so it can be called fresh each render
        // If it's a component, wrap it in a function
        if (typeof componentOrFunction === 'function') {
            context.setModal(componentOrFunction);
        } else {
            context.setModal(() => componentOrFunction);
        }
    };

    const hideModal = () => {
        context.setModal(null);
    }

    context.logout = handleLogOut;
    context.showModal = showModal;
    context.hideModal = hideModal;

    return (
        <SessionContext.Provider value={context}>
            <>
            {modal ? (typeof modal === 'function' ? modal() : modal) : <></>}
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