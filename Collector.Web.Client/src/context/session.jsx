import React, {useState} from 'react';
import {getUser, userContext} from './session/user';

// The Context 
const SessionContext = React.createContext({});

// Session Provider
const SessionProvider = ({children}) => {
    // context states
    const [user, setUser] = useState(getUser());

    // context properties passed to consumer
    let states = {
        ...userContext(user, setUser)
    };

    //actions
    const handleLogOut = () => {
        states.setUser(null);
    }

    states.logout = handleLogOut;

    return (
        <SessionContext.Provider value={states}>
            {children}
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