import React, {useState, useEffect} from 'react';
import {getUser, userContext} from './session/user';

// The Context 
const SessionContext = React.createContext({});

// Session Provider
const SessionProvider = ({children}) => {
    // context states
    const [user, setUser] = useState(getUser());
    const [modal, setModal] = useState(null);
    const [customModulesJournalId, setCustomModulesJournalId] = useState(null);

    // context properties passed to consumer
    const context = {
        ...userContext(user, setUser),
        modal,
        setModal,
        customModulesJournalId
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

    const loadCustomModulesJournal = async () => {
        if (customModulesJournalId) return; // Already loaded
        if (!user) return; // User not logged in
        
        try {
            const { Journals } = await import('@/api/user/journals');
            const { getOrCreateCustomModulesJournal } = Journals(context);
            const response = await getOrCreateCustomModulesJournal();
            
            if (response.data.success) {
                const journal = response.data.data;
                setCustomModulesJournalId(journal.id);
            } else {
                console.error('Failed to load custom modules journal:', response.data.message);
            }
        } catch (err) {
            console.error('Error loading custom modules journal:', err);
        }
    };

    // Load custom modules journal when user logs in
    useEffect(() => {
        if (user && !customModulesJournalId) {
            loadCustomModulesJournal();
        }
    }, [user]);

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